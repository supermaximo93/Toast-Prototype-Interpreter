using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using MathsLanguage.Types;
using MathsLanguage.Types.Singletons;

namespace MathsLanguage
{
    class Interpreter
    {
        public class ProgramStack
        {
            private Stack<Dictionary<string, MVariable>> stack;

            private int level;
            public int Level { get { return level; } }

            public ProgramStack()
            {
                stack = new Stack<Dictionary<string, MVariable>>();
                stack.Push(new Dictionary<string, MVariable>());
                level = 0;
            }

            public void Push()
            {
                stack.Push(new Dictionary<string, MVariable>());
                ++level;
            }

            public void Pop()
            {
                if (level == 0) return;
                stack.Pop();
                --level;
            }

            public void AddVariable(MVariable variable)
            {
                stack.Peek().Add(variable.Identifier, variable);
            }

            public MVariable FindVariable(string name)
            {
                MVariable variable;
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

        MBlock currentBlock;
        public MBlock CurrentBlock
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

            System.Console.WriteLine("Maths Language Interpreter version 0.1 by Max Foster\n");
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
            
            while ((Interpret(GetInput()).TypeName != MType.M_EXCEPTION_TYPENAME) && alive) ;

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
            MType.DIRECTIVE_CHARACTER.ToString(), MType.REFERENCE_CHARACTER.ToString(), MType.DEREFERENCE_CHARACTER.ToString(),
            "let", "yes", "no", "nil", "if", "otherwise", "begin", "end", "while", "for", "break", "or", "and"
        };
        public static readonly string[] SYMBOLS_TO_SPLIT_BY = new string[] {
            "^", "/", "*", "+", "-", "~=", ",", "|", ">", "<", ">=", "<=", "=", "/=",  "\"", "{", "}", "[", "]", "(", ")",
            MType.DIRECTIVE_CHARACTER.ToString(), MType.REFERENCE_CHARACTER.ToString(), MType.DEREFERENCE_CHARACTER.ToString()
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

        public Group SplitIntoSymbols(string command, out MException exception)
        {
            exception = null;
            command = command.Trim();

            List<string> strings = new List<string>();
            int index = -1;
            while ((index = command.IndexOf(MType.STRING_CHARACTER, index + 1)) >= 0)
            {
                int index2 = command.IndexOf(MType.STRING_CHARACTER, index + 1);
                if (index2 < 0)
                {
                    exception = new MException(this, "String not closed", "another \" required");
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
                    currentGroup.Add(new MString(strings[stringId]));
                    ++stringId;
                }
                else currentGroup.Add(s);
            }

            if (groupDepth > 0) new MException(this, "Too few closing brackets", groupDepth.ToString() + " required");
            else if (groupDepth < 0) new MException(this, "Too many closing brackets", "remove " + (-groupDepth).ToString());

            return superGroup;
        }

        public MType Interpret(string command, bool isFunctionCall = false)
        {
            command = command.Trim();

            if (command == "quit")
            {
                if (RunningFromFile && runningFromCommandLine)
                {
                    file.Close();
                    file = null;
                    System.Console.WriteLine();
                    return MNil.Instance;
                }
                return new MException(this, "Halting interpreter execution");
            }
            if (command == "") return MNil.Instance;
            if (command.StartsWith("//")) return MNil.Instance;

            MException exception;
            Group group = SplitIntoSymbols(command, out exception);
            if (exception != null) return exception;

            MType value = ParseGroup(group);
            if (value != null)
            {
                if (isFunctionCall) return value;

                exception = value as MException;
                if (exception == null)
                {
                    if (!RunningFromFile) System.Console.WriteLine("-> {0}", value.ToCSString());
                }
                else System.Console.WriteLine(exception.ToCSString());
            }

            if ((currentBlock == null) && !RunningFromFile) System.Console.WriteLine();
            return MNil.Instance;
        }

        public MType ParseGroup(Group group)
        {
            if (group.Count == 0) return new MArgumentList();

            string firstSymbolStr = group[0] as string;
            if (firstSymbolStr != null)
            {
                if (firstSymbolStr == MType.DIRECTIVE_CHARACTER.ToString())
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

                                        MType value = ParseGroup(conditionGroup);
                                        if (value is MException) return value;

                                        MBoolean result = value as MBoolean;
                                        if (result == null) return new MException(this, "Directive 'STRICT' could not be used",
                                            "invalid parameter; use yes or no");

                                        strict = result.Value;
                                        if (strict) System.Console.WriteLine("Interpreter running in strict mode");
                                        else System.Console.WriteLine("Interpreter not running in strict mode");

                                        return MNil.Instance;
                                    }
                                    else return new MException(this, "Directive 'STRICT' could not be used",
                                        "invalid parameter (none given); use yes or no");
                            }
                        }
                    }
                    return new MException(this, "Could not use directive", "directive '" + directive + "' not recognised");
                }

                switch (firstSymbolStr)
                {
                    case "let":
                        
                        int equalsIndex = group.IndexOf("=");
                        if (equalsIndex < 0) return new MException(this, "Variable or function could not be assigned a value");
                        int refIndex;

                        while ((refIndex = group.IndexOf(MType.REFERENCE_CHARACTER.ToString())) >= 0)
                        {
                            if (refIndex > equalsIndex) break;
                            if (refIndex + 1 > group.Count)
                                return new MException(this, "Invalid expression term '" + MType.REFERENCE_CHARACTER + "'");

                            MType variable = MType.Parse(this, group[refIndex + 1]);
                            if (variable is MException) return variable;
                            if (!(variable is MVariable)) return new MException(this, "Attempted creation of reference to value",
                                "expected variable identifier");

                            group[refIndex] = new MVariable("reference", variable);
                            group.RemoveAt(refIndex + 1);
                        }

                        while ((refIndex = group.IndexOf(MType.DEREFERENCE_CHARACTER.ToString())) >= 0)
                        {
                            if (refIndex > equalsIndex) break;
                            if (refIndex + 1 > group.Count)
                                return new MException(this, "Invalid expression term '" + MType.DEREFERENCE_CHARACTER + "'");

                            MType variable = MType.Parse(this, group[refIndex + 1]);
                            if (variable is MException) return variable;
                            if (!(variable is MVariable)) return new MException(this, "Attempted dereference of value",
                                "expected variable identifier");

                            group[refIndex] = ((MVariable)variable).Value;
                            if (!((MType)group[refIndex] is MVariable) && !((MType)group[refIndex] is MFunction))
                                return new MException(this, "Dereference of value type variable", "expected reference variable");
                            group.RemoveAt(refIndex + 1);
                        }

                        if (group.Count == 1) return new MException(this, "Could not assign variable", "no variable name given");

                        string variableName = group[1] as string;
                        MVariable existingVariable = null;

                        if (variableName == null)
                        {
                            MException exception = new MException(this, "Could not assign variable", "invalid variable name given");

                            Group groupToParse = group[1] as Group;
                            MType value = group[1] as MType;
                            if (groupToParse != null) value = ParseGroup(groupToParse);
                            if (value == null) return exception;

                            MVariable variable = value as MVariable;
                            if (variable != null) existingVariable = variable;
                            else
                            {
                                MFunction function = value as MFunction;
                                if (function != null) variableName = function.Name;
                                else return exception;
                            }
                        }

                        if (group.Count == 2) return new MException(this, "Variable could not be assigned a value");
                        string assignmentOperator = group[2] as string;
                        if (assignmentOperator == null)
                        {
                            Group paramGroup = group[2] as Group;
                            if (paramGroup == null) return new MException(this, "Variable could not be assigned a value",
                                "value to assign to variable must be given");

                            MParameterList paramList = new MParameterList();
                            bool commaExpected = false;
                            for (int i = 0; i < paramGroup.Count; ++i)
                            {
                                if (commaExpected && (i == paramGroup.Count - 1))
                                    return new MException(this, "Parameters could not be parsed", "last parameter missing");

                                string paramName = paramGroup[i] as string;

                                if (commaExpected && (paramName != ",")) paramName = null;

                                if (paramName == null) return new MException(this, "Parameters could not be parsed",
                                    "invalid parameter name given");

                                if (!commaExpected) paramList.Add(paramName);
                                commaExpected = !commaExpected;
                            }

                            MException exception = new MException(this, "Function could not be given a body",
                                "function body must be given");

                            if (group.Count == 3) return exception;
                            assignmentOperator = group[3] as string;
                            if (assignmentOperator == null) return exception;
                            else if (assignmentOperator != "=") return exception;

                            MFunction function;
                            if (group.Count == 4)
                            {
                                MBlock block = new MBlock(this, out exception, false);
                                if (exception != null) return exception;
                                function = new MFunction(variableName ?? existingVariable.Identifier, block,
                                    paramList.ParameterNames, null);
                            }
                            else
                            {
                                Group funcBody = new Group(null);
                                funcBody.AddRange(group.GetRange(4, group.Count - 4));
                                function = new MFunction(variableName ?? existingVariable.Identifier, funcBody.ToString(),
                                    paramList.ParameterNames, null);
                            }

                            exception = MFunction.AddFunction(this, function);
                            if (exception != null) return exception;

                            return function;
                        }
                        {
                            MException exception = new MException(this, "Variable could not be assigned a value",
                                "value to assign to variable must be given");

                            if (assignmentOperator != "=") return exception;
                            if (group.Count == 3) return exception;
                            Group valueGroup = new Group(null);
                            valueGroup.AddRange(group.GetRange(3, group.Count - 3));

                            MType value = ParseGroup(valueGroup);
                            if (value is MException) return value;

                            MVariable variable = value as MVariable;
                            if (variable != null) value = variable.Value;

                            variable = existingVariable ?? stack.FindVariable(variableName);
                            if (value == variable) return new MException(this, "Illegal assignment attempted",
                                "variables cannot reference themselves");
                            MVariable circularRefCheckVar = value as MVariable;
                            if (circularRefCheckVar != null)
                            {
                                if (circularRefCheckVar.Value == variable)
                                    return new MException(this, "Illegal assignment attempted", "circular reference detected");
                            }

                            if (variable == null)
                            {
                                variable = new MVariable(variableName, value);
                                Stack.AddVariable(variable);
                            }
                            else variable.Value = value;

                            return variable;
                        }

                    case "if":
                        {
                            if (group.Count == 1) return new MException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            int commaIndex = group.IndexOf(",");
                            if (commaIndex < 0) return new MException(this, "if statment invalid",
                                "comma required after condition");

                            if (group.Count == 1) return new MException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            Group conditionGroup = new Group(null);
                            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));

                            MType value = ParseGroup(conditionGroup);
                            if (value is MException) return value;

                            MBoolean result = value as MBoolean;
                            if (result == null) return new MException(this, "Condition does not evaluate to a boolean value",
                                "yes or no");

                            if (group.Count > commaIndex + 1)
                            {
                                int otherwiseIndex = group.IndexOf("otherwise", commaIndex + 1);

                                MBlock otherwiseBlock = null;
                                if (otherwiseIndex == group.Count - 1)
                                {
                                    MException exception;
                                    otherwiseBlock = new MBlock(this, out exception, false);
                                    if (exception != null) return exception;
                                }

                                if (result.Value)
                                {
                                    Group statementGroup = new Group(null);
                                    if (otherwiseIndex < 0)
                                        statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
                                    else statementGroup.AddRange(group.GetRange(commaIndex + 1, otherwiseIndex - (commaIndex + 1)));

                                    if (statementGroup.Count > 0) return ParseGroup(statementGroup);
                                }
                                else if (otherwiseIndex >= 0)
                                {
                                    if (otherwiseBlock == null)
                                    {
                                        Group statementGroup = new Group(null);
                                        statementGroup.AddRange(group.GetRange(otherwiseIndex + 1, group.Count - (otherwiseIndex + 1)));
                                        if (statementGroup.Count > 0) return ParseGroup(statementGroup);
                                    }
                                    else return otherwiseBlock.Execute(this);
                                }
                                else return result;
                            }

                            {
                                MException exception;
                                MBlock block = new MBlock(this, out exception, true);
                                if (exception != null) return exception;
                                if (result.Value) return block.Execute(this, true);
                                else if (block.HasOtherwise()) return block.Execute(this, false);
                            }
                            return result;
                        }

                    case "begin":
                        {
                            MException exception;
                            MType block = new MBlock(this, out exception, false);
                            return exception ?? block;
                        }

                    case "end":
                        return new MException(this, "Unexpected keyword 'end'");

                    case "otherwise":
                        return new MException(this, "Unexpected keyword 'otherwise'");

                    case "while":
                        {
                            if (group.Count == 1) return new MException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            int commaIndex = group.IndexOf(",");
                            if (commaIndex < 0) return new MException(this, "if statment invalid",
                                "comma required after condition");

                            if (group.Count == 1) return new MException(this, "Statement could not be evaluated",
                                "if statement must be given a condition");

                            Group conditionGroup = new Group(null);
                            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));

                            Group statementGroup = null;
                            MBlock block = null;
                            if (group.Count > commaIndex + 1)
                            {
                                statementGroup = new Group(null);
                                statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
                            }
                            else
                            {
                                MException exception;
                                block = new MBlock(this, out exception, false);
                                if (exception != null) return exception;
                            }

                            while (true)
                            {
                                MType value = ParseGroup((Group)conditionGroup.Clone());
                                if (value is MException) return value;

                                MBoolean result = value as MBoolean;
                                if (result == null) return new MException(this, "Condition does not evaluate to a boolean value",
                                    "yes or no");

                                if (result.Value)
                                {
                                    MType returnValue;
                                    if (statementGroup != null) returnValue = ParseGroup((Group)statementGroup.Clone());
                                    else returnValue = block.Execute(this);
                                    if (returnValue is MException) return returnValue;
                                    if (returnValue is MBreak) return MNil.Instance;
                                }
                                else return MNil.Instance;

                                if (!alive) return MNil.Instance;
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
                    MType value = ParseGroup(nextGroup);
                    if (value is MException) return value;
                    group[i] = value;
                }

                MType argument = group[i] as MType;
                if (argument != null)
                {
                    if (i - 1 >= 0)
                    {
                        bool functionCalled = false;
                        MType functionReturnValue = null;

                        MType value = MType.Parse(this, group[i - 1]);
                        MFunction function = value as MFunction;
                        if (function != null)
                        {
                            functionReturnValue = function.Call(this, argument);
                            functionCalled = true;
                        }
                        else
                        {
                            MBlock block = value as MBlock;
                            if (function != null)
                            {
                                functionReturnValue = block.Execute(this);
                                functionCalled = true;
                            }
                            else
                            {
                                MVariable variable = value as MVariable;
                                if (variable != null)
                                {
                                    function = variable.Value as MFunction;
                                    if (function != null)
                                    {
                                        functionReturnValue = function.Call(this, argument);
                                        functionCalled = true;
                                    }
                                    else
                                    {
                                        block = variable.Value as MBlock;
                                        if (block != null)
                                        {
                                            functionReturnValue = block.Execute(this);
                                            functionCalled = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (functionCalled)
                        {
                            if (functionReturnValue is MException) return functionReturnValue;
                            group[i - 1] = functionReturnValue;
                            group.RemoveAt(i);
                            --i;
                        }
                    }
                }
            }

            int index;

            while ((index = group.LastIndexOf(MType.REFERENCE_CHARACTER.ToString())) >= 0)
            {
                if (index + 1 > group.Count)
                    return new MException(this, "Invalid expression term '" + MType.REFERENCE_CHARACTER + "' at end of statement");

                MType variable = MType.Parse(this, group[index + 1]);
                if (variable is MException) return variable;
                if (!(variable is MVariable)) return new MException(this, "Attempted creation of reference to value", "expected variable identifier");

                group[index] = new MVariable("reference", variable);
                group.RemoveAt(index + 1);
            }

            while ((index = group.LastIndexOf(MType.DEREFERENCE_CHARACTER.ToString())) >= 0)
            {
                if (index + 1 > group.Count)
                    return new MException(this, "Invalid expression term '" + MType.DEREFERENCE_CHARACTER + "' at end of statement");

                MType variable = MType.Parse(this, group[index + 1]);
                if (variable is MException) return variable;
                if (!(variable is MVariable)) return new MException(this, "Attempted dereference of value", "expected variable identifier");

                group[index] = ((MVariable)variable).Value;
                if (!((MType)group[index] is MVariable) && !((MType)group[index] is MFunction))
                    return new MException(this, "Dereference of value type variable", "expected reference variable");
                group.RemoveAt(index + 1);
            }

            while ((index = group.IndexOf("-")) >= 0)
            {
                if (index + 1 >= group.Count) return new MException(this, "Invalid expression term '-'");

                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                bool aExists = false;
                if (index - 1 >= 0)
                {
                    MType a = MType.Parse(this, group[index - 1]);
                    if (!(a is MException))
                    {
                        group[index] = "+";
                        aExists = true;
                    }
                }

                MNumber number = b as MNumber;
                if (number == null)
                {
                    MException exception = new MException(this, "Failed to make value negative",
                        "Values of type '" + b.TypeName + "' cannot be made negative");
                    MVariable variable = b as MVariable;
                    if (variable == null) return exception;

                    number = variable.Value as MNumber;
                    if (number == null) return exception;
                }

                if (!aExists)
                {
                    group[index] = number.ToNegative();
                    group.RemoveAt(index + 1);
                }
                else group[index + 1] = number.ToNegative();
            }
            
            while ((index = group.IndexOf(MType.MODULUS_CHARACTER.ToString())) >= 0)
            {
                MException exception = new MException(this, "Modulus brackets not closed", "another | required");
                if (index + 2 >= group.Count) return exception;
                if (group[index + 2] is string)
                {
                    if ((string)group[index + 2] != MType.MODULUS_CHARACTER.ToString()) return exception;
                }
                else return exception;

                MType value = MType.Parse(this, group[index + 1]);
                if (value is MException) return value;

                MType result = Operations.Math.Modulus(this, value);
                if (result == null) return new MException(this, "Modulus operation failed", "reason unknown");
                if (result is MException) return result;
                group[index] = result;
                group.RemoveRange(index + 1, 2);
            }

            while ((index = group.IndexOf("^")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '^'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Pow(this, a, b);
                if (result == null) return new MException(this, "Exponentiation operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("/")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '/'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Divide(this, a, b);
                if (result == null) return new MException(this, "Division operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("*")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '*'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Multiply(this, a, b);
                if (result == null) return new MException(this, "Multiplication operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("+")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '+'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Add(this, a, b);
                if (result == null) return new MException(this, "Addition operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '='");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Equal(this, a, b, true);
                if (result == null) return new MException(this, "Comparison operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("~=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '~='");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Equal(this, a, b, false);
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("/=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '/='");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.NotEqual(this, a, b);
                if (result == null) return new MException(this, "Comparison operation failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '<'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Inequality(this, a, b, "<");
                if (result == null) return new MException(this, "Less than comparison failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '<'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Inequality(this, a, b, ">");
                if (result == null) return new MException(this, "Greater than comparison failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '<'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Inequality(this, a, b, "<=");
                if (result == null) return new MException(this, "Less than or equal comparison failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term '<'");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MType result = Operations.Math.Inequality(this, a, b, ">=");
                if (result == null) return new MException(this, "Greater than or equal comparison failed", "reason unknown");
                if (result is MException) return result;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(",")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term ','");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MArgumentList argList = a as MArgumentList;
                if (argList == null)
                {
                    argList = new MArgumentList();
                    argList.Add(a);
                    argList.Add(b);
                }
                else argList.Add(b);

                group[index - 1] = argList;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("and")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term ','");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MBoolean aBool = a as MBoolean;
                if (aBool == null) return new MException(this, "Left hand side of expression must be a boolean value", "yes or no");
                MBoolean bBool = b as MBoolean;
                if (bBool == null) return new MException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new MBoolean(aBool.Value && bBool.Value);
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("or")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count)) return new MException(this, "Invalid expression term ','");

                MType a = MType.Parse(this, group[index - 1]);
                if (a is MException) return a;
                MType b = MType.Parse(this, group[index + 1]);
                if (b is MException) return b;

                MBoolean aBool = a as MBoolean;
                if (aBool == null) return new MException(this, "Left hand side of expression must be a boolean value", "yes or no");
                MBoolean bBool = b as MBoolean;
                if (bBool == null) return new MException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new MBoolean(aBool.Value || bBool.Value);
                group.RemoveRange(index, 2);
            }

            if (group.Count == 0) return MNil.Instance;
            else if (group.Count == 1) return MType.Parse(this, group[0]);
            else return new MException(this, "Statement could not be evaluated completely");
        }

    }
}
