using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Toast.Types;
using Toast.Types.Singletons;

namespace Toast
{
    class Interpreter
    {
        public class ProgramStack
        {
            private Stack<Dictionary<string, TVariable>> stack;

            private int level;
            public int Level { get { return level; } }

            public ProgramStack()
            {
                stack = new Stack<Dictionary<string, TVariable>>();
                stack.Push(new Dictionary<string, TVariable>());
                level = 0;
            }

            public void Push()
            {
                stack.Push(new Dictionary<string, TVariable>());
                ++level;
            }

            public void Pop()
            {
                if (level == 0) return;
                stack.Pop();
                --level;
            }

            public void AddVariable(TVariable variable)
            {
                stack.Peek().Add(variable.Identifier, variable);
            }

            public TVariable FindVariable(string name)
            {
                TVariable variable;
                if (stack.Peek().TryGetValue(name, out variable)) return variable;
                return null;
            }
        }

        private ProgramStack stack;
        public ProgramStack Stack { get { return stack; } }

        private bool alive;
        public void Kill()
        {
            alive = false;
        }

        private bool strict;
        public bool Strict { get { return strict; } }

        System.IO.StreamReader file;
        public bool RunningFromFile { get { return file != null; } }
        private bool runningFromCommandLine;

        private int currentLine;
        public int CurrentLine { get { return currentLine; } }

        TBlock currentBlock;
        public TBlock CurrentBlock
        {
            get { return currentBlock; }
            set { currentBlock = value; }
        }

        public Interpreter()
        {
            stack = new ProgramStack();
            alive = true;
            strict = false;
            currentLine = 0;
            currentBlock = null;
            file = null;

            System.Console.WriteLine("Toast Interpreter version 0.1 by Max Foster\n");
        }

        public void Run(string fileName)
        {
            fileName = fileName.Trim();
            if (fileName == "") runningFromCommandLine = true;
            else
            {
                runningFromCommandLine = false;
                if (System.IO.File.Exists(fileName)) file = System.IO.File.OpenText(fileName);
                else
                {
                    System.Console.WriteLine("File '{0}' not found", fileName);
                    System.Console.ReadLine();
                    return;
                }
            }
            
            while ((Interpret(GetInput()).TypeName != TType.T_EXCEPTION_TYPENAME) && alive) ;

            if (file != null) file.Close();
            System.Console.WriteLine("\nInterpreter execution halted");
            System.Console.ReadLine();
        }

        public void LoadFile(string fileName)
        {
            fileName = fileName.Trim();
            if (fileName != "")
            {
                if (System.IO.File.Exists(fileName)) file = System.IO.File.OpenText(fileName);
                else System.Console.WriteLine("File '{0}' not found", fileName);
            }
        }

        public string GetInput()
        {
            if (currentBlock != null)
            {
                ++currentLine;
                return currentBlock.GetInput();
            }
            else if (RunningFromFile)
            {
                ++currentLine;
                return file.EndOfStream ? "quit" : file.ReadLine();
            }
            else
            {
                System.Console.Write("{0} > ", currentLine);
                ++currentLine;
                return System.Console.ReadLine();
            }
        }

        private const string CONTROL_STATEMENT_LIST_STRING = "STRICT";
        public static readonly string[] RESERVED_SYMBOLS = new string[] {
            "^", "/", "*", "+", "-", "~=", ",", "|", ">", "<", ">=", "<=", "=", "/=", "\"", "{", "}", "[", "]", "(", ")",
            TType.DIRECTIVE_CHARACTER.ToString(), TType.REFERENCE_CHARACTER.ToString(), TType.DEREFERENCE_CHARACTER.ToString(),
            "let", "yes", "no", "nil", "if", "else", "begin", "end", "while", "for", "break", "or", "and"
        };
        public static readonly string[] SYMBOLS_TO_SPLIT_BY = new string[] {
            "^", "/", "*", "+", "-", "~=", ",", "|", ">", "<", ">=", "<=", "=", "/=",  "\"", "{", "}", "[", "]", "(", ")",
            TType.DIRECTIVE_CHARACTER.ToString(), TType.REFERENCE_CHARACTER.ToString(), TType.DEREFERENCE_CHARACTER.ToString()
        };

        public class Group : ArrayList
        {
            private Group parentGroup;
            public Group ParentGroup { get { return parentGroup; } }

            public Group(Group parentGroup)
            {
                this.parentGroup = parentGroup;
                if (parentGroup != null) parentGroup.Add(this);
            }

            public override string ToString()
            {
                string returnStr = "( ";
                for (int i = 0; i < Count; ++i) returnStr += this[i] + " ";
                return returnStr + ")";
            }

            public override object Clone()
            {
                Group copy = new Group(null);
                copy.parentGroup = parentGroup;
                copy.AddRange(this);
                return copy;
            }
        }

        public Group SplitIntoSymbols(string command, out TException exception)
        {
            exception = null;
            command = command.Trim();

            List<string> strings = new List<string>();
            int index = -1;
            while ((index = command.IndexOf(TType.STRING_CHARACTER, index + 1)) >= 0)
            {
                int index2 = command.IndexOf(TType.STRING_CHARACTER, index + 1);
                if (index2 < 0)
                {
                    exception = new TException(this, "String not closed", "another \" required");
                    return null;
                }

                string str = command.Substring(index + 1, (index2 - index) - 1);
                command = command.Remove(index + 1, index2 - index);
                strings.Add(str);
            }

            foreach (string s in SYMBOLS_TO_SPLIT_BY)
            {
                string newS = " " + s + " ";
                command = command.Replace(s, newS); // 2 character symbols are broken, fix ahead...
            }
            List<string> symbols = new List<string>();
            foreach (string s in command.Split(' '))
            {
                string str = s.Trim();
                if (str != "") symbols.Add(str);
            }

            Group superGroup = new Group(null);
            Group currentGroup = superGroup;
            int groupDepth = 0, stringId = 0;
            bool modulusOpen = false;

            for (int i = 0; i < symbols.Count; ++i)
            {
                if (symbols[i] == "") continue;

                // Fix for broken 2 character symbols
                if (i + 1 < symbols.Count)
                {
                    if (symbols[i + 1] == "=")
                    {
                        if (symbols[i] == "~")
                        {
                            symbols[i] = "~=";
                            symbols.RemoveAt(i + 1);
                        }
                        else if (symbols[i] == ">")
                        {
                            symbols[i] = ">=";
                            symbols.RemoveAt(i + 1);
                        }
                        else if (symbols[i] == "<")
                        {
                            symbols[i] = "<=";
                            symbols.RemoveAt(i + 1);
                        }
                        else if (symbols[i] == "/")
                        {
                            symbols[i] = "/=";
                            symbols.RemoveAt(i + 1);
                        }
                    }
                }

                string s = symbols[i];

                if ((s == "(") || ((s == "|") && !modulusOpen))
                {
                    if (s == "|")
                    {
                        modulusOpen = true;
                        currentGroup.Add(s);
                    }
                    Group newGroup = new Group(currentGroup);
                    currentGroup = newGroup;
                    ++groupDepth;
                }
                else if ((s == ")") || ((s == "|") && modulusOpen))
                {
                    if (currentGroup.ParentGroup != null) currentGroup = currentGroup.ParentGroup;
                    --groupDepth;
                    if (s == "|")
                    {
                        modulusOpen = false;
                        currentGroup.Add(s);
                    }
                }
                else if (s == "\"")
                {
                    currentGroup.Add(new TString(strings[stringId]));
                    ++stringId;
                }
                else currentGroup.Add(s);
            }

            if (groupDepth > 0) new TException(this, "Too few closing brackets", groupDepth.ToString() + " required");
            else if (groupDepth < 0) new TException(this, "Too many closing brackets", "remove " + (-groupDepth).ToString());

            return superGroup;
        }

        public TType Interpret(string command, bool isFunctionCall = false)
        {
            command = command.Trim();

            if (command == "quit")
            {
                if (RunningFromFile && runningFromCommandLine)
                {
                    file.Close();
                    file = null;
                    System.Console.WriteLine();
                    return TNil.Instance;
                }
                return new TException(this, "Halting interpreter execution");
            }
            if (command == "") return TNil.Instance;
            if (command.StartsWith("//")) return TNil.Instance;

            TException exception;
            Group group = SplitIntoSymbols(command, out exception);
            if (exception != null) return exception;

            TType value = ParseGroup(group);
            if (value != null)
            {
                if (isFunctionCall) return value;

                exception = value as TException;
                if (exception == null)
                {
                    if (!RunningFromFile) System.Console.WriteLine("-> {0}", value.ToCSString());
                }
                else System.Console.WriteLine(exception.ToCSString());
            }

            if ((currentBlock == null) && !RunningFromFile) System.Console.WriteLine();
            return TNil.Instance;
        }

        public TType ParseGroup(Group group)
        {
            if (group.Count == 0) return new TArgumentList();

            string firstSymbolStr = group[0] as string;
            if (firstSymbolStr != null)
            {
                if (firstSymbolStr == TType.DIRECTIVE_CHARACTER.ToString())
                {
                    string directive = "";
                    if (group.Count > 1)
                    {
                        directive = group[1] as string;
                        if (directive == null) directive = "";
                        else
                        {
                            switch (directive)
                            {
                                case "STRICT":
                                    if (group.Count > 2)
                                    {
                                        Group conditionGroup = new Group(null);
                                        conditionGroup.AddRange(group.GetRange(2, group.Count - 2));

                                        TType value = ParseGroup(conditionGroup);
                                        if (value is TException) return value;

                                        TBoolean result = value as TBoolean;
                                        if (result == null) return new TException(this, "Directive 'STRICT' could not be used",
                                            "invalid parameter; use yes or no");

                                        strict = result.Value;
                                        if (strict) System.Console.WriteLine("Interpreter running in strict mode");
                                        else System.Console.WriteLine("Interpreter not running in strict mode");

                                        return TNil.Instance;
                                    }
                                    else return new TException(this, "Directive 'STRICT' could not be used",
                                        "invalid parameter (none given); use yes or no");
                            }
                        }
                    }
                    return new TException(this, "Could not use directive", "directive '" + directive + "' not recognised");
                }

                switch (firstSymbolStr)
                {
                    case "let":
                        
                        int equalsIndex = group.IndexOf("=");
                        if (equalsIndex < 0) return new TException(this, "Variable or function could not be assigned a value");
                        int refIndex;

                        while ((refIndex = group.IndexOf(TType.REFERENCE_CHARACTER.ToString())) >= 0)
                        {
                            if (refIndex > equalsIndex) break;
                            if (refIndex + 1 > group.Count)
                                return new TException(this, "Invalid expression term '" + TType.REFERENCE_CHARACTER + "'");

                            TType variable = TType.Parse(this, group[refIndex + 1]);
                            if (variable is TException) return variable;
                            if (!(variable is TVariable)) return new TException(this, "Attempted creation of reference to value",
                                "expected variable identifier");

                            group[refIndex] = new TVariable("reference", variable);
                            group.RemoveAt(refIndex + 1);
                        }

                        while ((refIndex = group.IndexOf(TType.DEREFERENCE_CHARACTER.ToString())) >= 0)
                        {
                            if (refIndex > equalsIndex) break;
                            if (refIndex + 1 > group.Count)
                                return new TException(this, "Invalid expression term '" + TType.DEREFERENCE_CHARACTER + "'");

                            TType variable = TType.Parse(this, group[refIndex + 1]);
                            if (variable is TException) return variable;
                            if (!(variable is TVariable)) return new TException(this, "Attempted dereference of value",
                                "expected variable identifier");

                            group[refIndex] = ((TVariable)variable).Value;
                            if (!((TType)group[refIndex] is TVariable) && !((TType)group[refIndex] is TFunction))
                                return new TException(this, "Dereference of value type variable", "expected reference variable");
                            group.RemoveAt(refIndex + 1);
                        }

                        if (group.Count == 1) return new TException(this, "Could not assign variable", "no variable name given");

                        string variableName = group[1] as string;
                        TVariable existingVariable = null;

                        if (variableName == null)
                        {
                            TException exception = new TException(this, "Could not assign variable", "invalid variable name given");

                            Group groupToParse = group[1] as Group;
                            TType value = group[1] as TType;
                            if (groupToParse != null) value = ParseGroup(groupToParse);
                            if (value == null) return exception;

                            TVariable variable = value as TVariable;
                            if (variable != null) existingVariable = variable;
                            else
                            {
                                TFunction function = value as TFunction;
                                if (function != null) variableName = function.Name;
                                else return exception;
                            }
                        }

                        if (group.Count == 2) return new TException(this, "Variable could not be assigned a value");
                        string assignmentOperator = group[2] as string;
                        if (assignmentOperator == null)
                        {
                            Group paramGroup = group[2] as Group;
                            if (paramGroup == null) return new TException(this, "Variable could not be assigned a value",
                                "value to assign to variable must be given");

                            TParameterList paramList = new TParameterList();
                            bool commaExpected = false;
                            for (int i = 0; i < paramGroup.Count; ++i)
                            {
                                if (commaExpected && (i == paramGroup.Count - 1))
                                    return new TException(this, "Parameters could not be parsed", "last parameter missing");

                                string paramName = paramGroup[i] as string;

                                if (commaExpected && (paramName != ",")) paramName = null;

                                if (paramName == null) return new TException(this, "Parameters could not be parsed",
                                    "invalid parameter name given");

                                if (!commaExpected) paramList.Add(paramName);
                                commaExpected = !commaExpected;
                            }

                            TException exception = new TException(this, "Function could not be given a body",
                                "function body must be given");

                            if (group.Count == 3) return exception;
                            assignmentOperator = group[3] as string;
                            if (assignmentOperator == null) return exception;
                            else if (assignmentOperator != "=") return exception;

                            TFunction function;
                            if (group.Count == 4)
                            {
                                TBlock block = new TBlock(this, out exception, false);
                                if (exception != null) return exception;
                                function = new TFunction(variableName ?? existingVariable.Identifier, block,
                                    paramList.ParameterNames, null);
                            }
                            else
                            {
                                Group funcBody = new Group(null);
                                funcBody.AddRange(group.GetRange(4, group.Count - 4));
                                function = new TFunction(variableName ?? existingVariable.Identifier, funcBody.ToString(),
                                    paramList.ParameterNames, null);
                            }

                            exception = TFunction.AddFunction(this, function);
                            if (exception != null) return exception;

                            return function;
                        }
                        {
                            TException exception = new TException(this, "Variable could not be assigned a value",
                                "value to assign to variable must be given");

                            if (assignmentOperator != "=") return exception;
                            if (group.Count == 3) return exception;
                            Group valueGroup = new Group(null);
                            valueGroup.AddRange(group.GetRange(3, group.Count - 3));

                            TType value = ParseGroup(valueGroup);
                            if (value is TException) return value;

                            TVariable variable = value as TVariable;
                            if (variable != null) value = variable.Value;

                            variable = existingVariable ?? stack.FindVariable(variableName);
                            if (value == variable) return new TException(this, "Illegal assignment attempted",
                                "variables cannot reference themselves");

                            TVariable circularRefCheckVar = value as TVariable;
                            while (circularRefCheckVar != null)
                            {
                                TVariable variableValue = circularRefCheckVar.Value as TVariable;
                                if (variableValue != null)
                                {
                                    if (variableValue == variable)
                                        return new TException(this, "Illegal assignment attempted",
                                            "circular reference detected");
                                    else circularRefCheckVar = variableValue;
                                }
                                else circularRefCheckVar = null;
                            }

                            if (variable == null)
                            {
                                variable = new TVariable(variableName, value);
                                Stack.AddVariable(variable);
                            }
                            else variable.Value = value;

                            return variable;
                        }

                    case "if":
                        {
                            if (group.Count == 1) return new TException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            int commaIndex = group.IndexOf(",");
                            if (commaIndex < 0) return new TException(this, "if statment invalid",
                                "comma required after condition");

                            if (group.Count == 1) return new TException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            Group conditionGroup = new Group(null);
                            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));

                            TType value = ParseGroup(conditionGroup);
                            if (value is TException) return value;

                            TBoolean result = value as TBoolean;
                            if (result == null) return new TException(this, "Condition does not evaluate to a boolean value",
                                "yes or no");

                            if (group.Count > commaIndex + 1)
                            {
                                int elseIndex = group.IndexOf("else", commaIndex + 1);

                                TBlock elseBlock = null;
                                if (elseIndex == group.Count - 1)
                                {
                                    TException exception;
                                    elseBlock = new TBlock(this, out exception, false);
                                    if (exception != null) return exception;
                                }

                                if (result.Value)
                                {
                                    Group statementGroup = new Group(null);
                                    if (elseIndex < 0)
                                        statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
                                    else statementGroup.AddRange(group.GetRange(commaIndex + 1, elseIndex - (commaIndex + 1)));

                                    if (statementGroup.Count > 0) return ParseGroup(statementGroup);
                                }
                                else if (elseIndex >= 0)
                                {
                                    if (elseBlock == null)
                                    {
                                        Group statementGroup = new Group(null);
                                        statementGroup.AddRange(group.GetRange(elseIndex + 1, group.Count - (elseIndex + 1)));
                                        if (statementGroup.Count > 0) return ParseGroup(statementGroup);
                                    }
                                    else
                                    {
                                        bool exitFromFunction;
                                        return elseBlock.Execute(this, out exitFromFunction);
                                    }
                                }
                                else return result;
                            }

                            {
                                TException exception;
                                TBlock block = new TBlock(this, out exception, true);
                                if (exception != null) return exception;

                                bool exitFromFunction;
                                if (result.Value) return block.Execute(this, out exitFromFunction, true);
                                else if (block.HasOtherwise()) return block.Execute(this, out exitFromFunction, false);
                            }
                            return result;
                        }

                    case "begin":
                        {
                            TException exception;
                            TType block = new TBlock(this, out exception, false);
                            return exception ?? block;
                        }

                    case "end":
                        return new TException(this, "Unexpected keyword 'end'");

                    case "else":
                        return new TException(this, "Unexpected keyword 'else'");

                    case "while":
                        {
                            if (group.Count == 1) return new TException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            int commaIndex = group.IndexOf(",");
                            if (commaIndex < 0) return new TException(this, "if statment invalid",
                                "comma required after condition");

                            if (group.Count == 1) return new TException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            Group conditionGroup = new Group(null);
                            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));

                            Group statementGroup = null;
                            TBlock block = null;
                            if (group.Count > commaIndex + 1)
                            {
                                statementGroup = new Group(null);
                                statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
                            }
                            else
                            {
                                TException exception;
                                block = new TBlock(this, out exception, false);
                                if (exception != null) return exception;
                            }

                            while (true)
                            {
                                TType value = ParseGroup((Group)conditionGroup.Clone());
                                if (value is TException) return value;

                                TBoolean result = value as TBoolean;
                                if (result == null) return new TException(this, "Condition does not evaluate to a boolean value",
                                    "yes or no");

                                if (result.Value)
                                {
                                    TType returnValue;
                                    if (statementGroup != null) returnValue = ParseGroup((Group)statementGroup.Clone());
                                    else
                                    {
                                        bool exitFromFunction;
                                        returnValue = block.Execute(this, out exitFromFunction);
                                        if (exitFromFunction) return returnValue;
                                    }
                                    if (returnValue is TException) return returnValue;
                                    if (returnValue is TBreak) return TNil.Instance;
                                }
                                else return TNil.Instance;

                                if (!alive) return TNil.Instance;
                            }
                        }

                    case "for":
                        break;
                }
            }

            for (int i = 0; i < group.Count; ++i)
            {
                Group nextGroup = group[i] as Group;
                if (nextGroup != null)
                {
                    TType value = ParseGroup(nextGroup);
                    if (value is TException) return value;
                    group[i] = value;
                }

                TType argument = group[i] as TType;
                if (argument != null)
                {
                    if (i - 1 >= 0)
                    {
                        bool functionCalled = false;
                        TType functionReturnValue = null;

                        TType value = TType.Parse(this, group[i - 1]);
                        TFunction function = value as TFunction;
                        if (function != null)
                        {
                            functionReturnValue = function.Call(this, argument);
                            functionCalled = true;
                        }
                        else
                        {
                            TBlock block = value as TBlock;
                            if (function != null)
                            {
                                bool exitFromFunction;
                                functionReturnValue = block.Execute(this, out exitFromFunction);
                                if (exitFromFunction) return functionReturnValue;
                                functionCalled = true;
                            }
                            else
                            {
                                TVariable variable = value as TVariable;
                                if (variable != null)
                                {
                                    function = variable.Value as TFunction;
                                    if (function != null)
                                    {
                                        functionReturnValue = function.Call(this, argument);
                                        functionCalled = true;
                                    }
                                    else
                                    {
                                        block = variable.Value as TBlock;
                                        if (block != null)
                                        {
                                            bool exitFromFunction;
                                            functionReturnValue = block.Execute(this, out exitFromFunction);
                                            if (exitFromFunction) return functionReturnValue;
                                            functionCalled = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (functionCalled)
                        {
                            if (functionReturnValue is TException) return functionReturnValue;
                            group[i - 1] = functionReturnValue;
                            group.RemoveAt(i);
                            --i;
                        }
                    }
                }
            }

            int index;

            while ((index = group.LastIndexOf(TType.REFERENCE_CHARACTER.ToString())) >= 0)
            {
                if (index + 1 > group.Count)
                    return new TException(this, "Invalid expression term '" + TType.REFERENCE_CHARACTER + "' at end of statement");

                TType variable = TType.Parse(this, group[index + 1]);
                if (variable is TException) return variable;
                if (!(variable is TVariable)) return new TException(this, "Attempted creation of reference to value", "expected variable identifier");

                group[index] = new TVariable("reference", variable);
                group.RemoveAt(index + 1);
            }

            while ((index = group.LastIndexOf(TType.DEREFERENCE_CHARACTER.ToString())) >= 0)
            {
                if (index + 1 > group.Count)
                    return new TException(this, "Invalid expression term '" + TType.DEREFERENCE_CHARACTER + "' at end of statement");

                TType variable = TType.Parse(this, group[index + 1]);
                if (variable is TException) return variable;
                if (!(variable is TVariable)) return new TException(this, "Attempted dereference of value", "expected variable identifier");

                group[index] = ((TVariable)variable).Value;
                if (!((TType)group[index] is TVariable) && !((TType)group[index] is TFunction))
                    return new TException(this, "Dereference of value type variable", "expected reference variable");
                group.RemoveAt(index + 1);
            }

            while ((index = group.IndexOf("-")) >= 0)
            {
                if (index + 1 >= group.Count) return new TException(this, "Invalid expression term '-'");

                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                bool aExists = false;
                if (index - 1 >= 0)
                {
                    TType a = TType.Parse(this, group[index - 1]);
                    if (!(a is TException))
                    {
                        group[index] = "+";
                        aExists = true;
                    }
                }

                TNumber number = b as TNumber;
                if (number == null)
                {
                    TException exception = new TException(this, "Failed to make value negative",
                        "Values of type '" + b.TypeName + "' cannot be made negative");
                    TVariable variable = b as TVariable;
                    if (variable == null) return exception;

                    number = variable.Value as TNumber;
                    if (number == null) return exception;
                }

                if (!aExists)
                {
                    group[index] = number.ToNegative();
                    group.RemoveAt(index + 1);
                }
                else group[index + 1] = number.ToNegative();
            }
            
            while ((index = group.IndexOf(TType.MODULUS_CHARACTER.ToString())) >= 0)
            {
                TException exception = new TException(this, "Modulus brackets not closed", "another | required");
                if (index + 2 >= group.Count) return exception;
                if (group[index + 2] is string)
                {
                    if ((string)group[index + 2] != TType.MODULUS_CHARACTER.ToString()) return exception;
                }
                else return exception;

                TType value = TType.Parse(this, group[index + 1]);
                if (value is TException) return value;

                TType result = Operations.Math.Modulus(this, value);
                if (result == null) return new TException(this, "Modulus operation failed", "reason unknown");
                if (result is TException) return result;
                group[index] = result;
                group.RemoveRange(index + 1, 2);
            }

            while ((index = group.IndexOf("^")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '^'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Pow(this, a, b);
                if (result == null) return new TException(this, "Exponentiation operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("/")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '/'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Divide(this, a, b);
                if (result == null) return new TException(this, "Division operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("*")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '*'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Multiply(this, a, b);
                if (result == null) return new TException(this, "Multiplication operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("+")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '+'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Add(this, a, b);
                if (result == null) return new TException(this, "Addition operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Equal(this, a, b, true);
                if (result == null) return new TException(this, "Comparison operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("~=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '~='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Equal(this, a, b, false);
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("/=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '/='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.NotEqual(this, a, b);
                if (result == null) return new TException(this, "Comparison operation failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Inequality(this, a, b, "<");
                if (result == null) return new TException(this, "Less than comparison failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Inequality(this, a, b, ">");
                if (result == null) return new TException(this, "Greater than comparison failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Inequality(this, a, b, "<=");
                if (result == null) return new TException(this, "Less than or equal comparison failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TType result = Operations.Math.Inequality(this, a, b, ">=");
                if (result == null) return new TException(this, "Greater than or equal comparison failed", "reason unknown");
                if (result is TException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(",")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TArgumentList argList = a as TArgumentList;
                if (argList == null)
                {
                    argList = new TArgumentList();
                    argList.Add(a);
                    argList.Add(b);
                }
                else argList.Add(b);

                group[index - 1] = argList;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("and")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TBoolean aBool = a as TBoolean;
                if (aBool == null) return new TException(this, "Left hand side of expression must be a boolean value", "yes or no");
                TBoolean bBool = b as TBoolean;
                if (bBool == null) return new TException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new TBoolean(aBool.Value && bBool.Value);
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("or")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b;

                TBoolean aBool = a as TBoolean;
                if (aBool == null) return new TException(this, "Left hand side of expression must be a boolean value", "yes or no");
                TBoolean bBool = b as TBoolean;
                if (bBool == null) return new TException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new TBoolean(aBool.Value || bBool.Value);
                group.RemoveRange(index, 2);
            }

            if (group.Count == 0) return TNil.Instance;
            else if (group.Count == 1) return TType.Parse(this, group[0]);
            else return new TException(this, "Statement could not be evaluated completely");
        }

    }
}
