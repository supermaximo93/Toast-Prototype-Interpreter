using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Toast.Types;
using Toast.Types.Singletons;

namespace Toast
{
    /// <summary>
    /// A class that interprets Toast code.
    /// </summary>
    class Interpreter
    {
        /// <summary>
        /// A class that manages the stack 'frames'. The stack is pushed before a function call and popped when the
        /// function call is finished.
        /// </summary>
        public class ProgramStack
        {
            // Each stack 'frame' is just a dictionary of TVariables. TFunctions have global scope and shadow
            // TVariables
            Stack<Dictionary<string, TVariable>> stack;

            public int Level { get; private set; }

            public ProgramStack()
            {
                stack = new Stack<Dictionary<string, TVariable>>();
                stack.Push(new Dictionary<string, TVariable>());
                Level = 0;
            }

            /// <summary>
            /// Pushes the stack for a new function scope.
            /// </summary>
            public void Push()
            {
                stack.Push(new Dictionary<string, TVariable>());
                ++Level;
            }

            /// <summary>
            /// Pops the stack to return to the outer function's scope.
            /// </summary>
            public void Pop()
            {
                if (Level == 0) return;
                stack.Pop();
                --Level;
            }

            /// <summary>
            /// Adds a variable within the current Toast function's scope.
            /// </summary>
            /// <param name="variable">The TVariable to add.</param>
            public void AddVariable(TVariable variable)
            {
                stack.Peek().Add(variable.Identifier, variable);
            }

            /// <summary>
            /// Attempts to find a variable within the current Toast function's scope.
            /// </summary>
            /// <param name="name">The identifier of the variable to search for.</param>
            /// <returns>The TVariable that is being searched for, if successful. Returns null on failure.</returns>
            public TVariable FindVariable(string name)
            {
                TVariable variable;
                if (stack.Peek().TryGetValue(name, out variable)) return variable;
                return null;
            }
        }

        /// <summary>
        /// A class that contains a group of symbols to interpret.
        /// </summary>
        public class Group : ArrayList
        {
            public Group ParentGroup { get; private set; }

            /// <summary>
            /// The contructor for Interpreter.Group.
            /// </summary>
            /// <param name="parentGroup">
            /// The Group that contains this Group. If parentGroup is not null, then this group will be appended to
            /// parentGroup.
            /// </param>
            public Group(Group parentGroup)
            {
                ParentGroup = parentGroup;
                if (parentGroup != null) parentGroup.Add(this);
            }

            public override string ToString()
            {
                StringBuilder returnStr = new StringBuilder("( ");
                for (int i = 0; i < Count; ++i) returnStr.Append(this[i]).Append(" ");
                return returnStr.Append(")").ToString();
            }

            public override object Clone()
            {
                Group copy = new Group(null);
                copy.ParentGroup = ParentGroup; // Setting the ParentGroup separately avoids problems
                copy.AddRange(this);
                return copy;
            }
        }

        public ProgramStack Stack { get; private set; }
        public bool Strict { get; private set; } // If the interpreter is in strict mode, it will halt at any error
        public int CurrentLine { get; private set; }

        // If CurrentBlock is not null, then the interpreter will interpret the code of the block instead of
        // interpreting code from a file or the command line
        public TBlock CurrentBlock { get; set; }

        bool alive;
        /// <summary>
        /// Halts the interpreter execution.
        /// </summary>
        public void Kill()
        {
            alive = false;
        }

        System.IO.StreamReader file;
        public bool RunningFromFile { get { return file != null; } }
        bool runningFromCommandLine;

        public Interpreter()
        {
            Stack = new ProgramStack();
            alive = true;
            Strict = false;
            CurrentLine = 0;
            CurrentBlock = null;
            file = null;

            System.Console.WriteLine("Toast Interpreter prototype by Max Foster\n");
        }

        /// <summary>
        /// Runs the interpreter, executing code from a file if specified.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the script to execute. Pass an empty string to make the interpreter run from the command
        /// line.
        /// </param>
        public void Run(string fileName)
        {
            fileName = fileName.Trim();
            if (fileName == "") runningFromCommandLine = true;
            else
            {
                runningFromCommandLine = false;
                LoadFile(fileName);
                if (file == null)
                {
                    System.Console.ReadLine();
                    return;
                }
            }
            
            // Keep interpreting code until something kills the interpreter
            while ((Interpret(GetInput()).TypeName != TType.T_EXCEPTION_TYPENAME) && alive) ;

            if (file != null) file.Close();
            System.Console.WriteLine("\nInterpreter execution halted");
            System.Console.ReadLine();
        }

        /// <summary>
        /// Loads a file for the interpreter to execute. Useful for loading scripts while running in CLI mode.
        /// </summary>
        /// <param name="fileName">The file name of the script to load.</param>
        public void LoadFile(string fileName)
        {
            fileName = fileName.Trim();
            if (fileName != "")
            {
                if (System.IO.File.Exists(fileName)) file = System.IO.File.OpenText(fileName);
                else System.Console.WriteLine("File '{0}' not found", fileName);
            }
        }

        /// <summary>
        /// Gets the next line of code to interpret from a relevant input source, which could be (in order of
        /// presendence) a TBlock, a file or the CLI.
        /// </summary>
        /// <returns>A string containing the next line of code to interpret.</returns>
        public string GetInput()
        {
            if (CurrentBlock != null)
            {
                ++CurrentLine;
                return CurrentBlock.GetInput();
            }
            
            if (RunningFromFile)
            {
                ++CurrentLine;
                return file.EndOfStream ? "quit" : file.ReadLine();
            }

            ++CurrentLine;
            System.Console.Write("{0} > ", CurrentLine);
            return System.Console.ReadLine();
        }

        public static readonly string[] RESERVED_SYMBOLS = new string[] {
            "^", "/", "*", "+", "-", "~=", ",", "|", ">", "<", ">=", "<=", "=", "/=", "\"", "{", "}", "[", "]",
            "(", ")",
            TType.DIRECTIVE_CHARACTER.ToString(), TType.REFERENCE_CHARACTER.ToString(),
            TType.DEREFERENCE_CHARACTER.ToString(),
            "let", "yes", "no", "nil", "if", "else", "begin", "end", "while", "for", "break", "or", "and"
        };
        public static readonly string[] SYMBOLS_TO_SPLIT_BY = new string[] {
            "^", "/", "*", "+", "-", "~=", ",", "|", ">", "<", ">=", "<=", "=", "/=",  "\"", "{", "}", "[", "]",
            "(", ")",
            TType.DIRECTIVE_CHARACTER.ToString(), TType.REFERENCE_CHARACTER.ToString(),
            TType.DEREFERENCE_CHARACTER.ToString()
        };

        /// <summary>
        /// Splits a string into symbols and puts them in an Interpreter.Group.
        /// (This method is probably really inefficient with the way it handles strings...)
        /// </summary>
        /// <param name="statement">The string to split into symbols.</param>
        /// <param name="exception">A value that will be set if an error occurs.</param>
        /// <returns>The Interpreter.Group that contains the resulting symbols. Returns null on failure.</returns>
        public Group SplitIntoSymbols(string statement, out TException exception)
        {
            exception = null;
            statement = statement.Trim();

            // Extract the strings from the statement, storing them in a list
            List<string> strings = new List<string>();
            int index = -1;
            while ((index = statement.IndexOf(TType.STRING_CHARACTER, index + 1)) >= 0)
            {
                // Find the string terminator
                int index2 = statement.IndexOf(TType.STRING_CHARACTER, index + 1);
                if (index2 < 0)
                {
                    exception = new TException(this, "String not closed", "another \" required");
                    return null;
                }

                // Add the substring between the speech marks to the list, and leave the beginning speech mark of the
                // string in tact so that the strings can be substituded back into the final group
                string str = statement.Substring(index + 1, (index2 - index) - 1);
                statement = statement.Remove(index + 1, index2 - index);
                strings.Add(str);
            }

            // Split up the symbols
            foreach (string s in SYMBOLS_TO_SPLIT_BY)
            {
                string newS = " " + s + " ";
                statement = statement.Replace(s, newS); // 2 character symbols are broken, fix ahead...
            }

            // Populate a list with the symbols
            List<string> symbols = new List<string>();
            foreach (string s in statement.Split(' '))
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

                // If any opening brackets are found, create a new Group to contain the symbols within the brackets
                if ((s == "(") || ((s == "|") && !modulusOpen))
                {
                    if (s == "|") // If a modulus bracket is found, create a new group between the modulus brackets
                    {
                        modulusOpen = true;
                        currentGroup.Add(s);
                    }
                    Group newGroup = new Group(currentGroup);
                    currentGroup = newGroup;
                    ++groupDepth;
                }
                else if ((s == ")") || ((s == "|") && modulusOpen))
                {   // If any closing brackets are found, finish adding to the current group and start adding to the
                    // parent group
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

            // Return TExceptions if the brackets were not matched up properly
            if (groupDepth > 0)
            {
                exception = new TException(this, "Too few closing brackets", groupDepth.ToString() + " required");
                return null;
            }
            if (groupDepth < 0)
            {
                exception = new TException(this, "Too many closing brackets", "remove " + (-groupDepth).ToString());
                return null;
            }

            return superGroup;
        }

        /// <summary>
        /// Interprets a line of Toast code.
        /// </summary>
        /// <param name="statement">A string containing the line of Toast code to interpret.</param>
        /// <param name="isFunctionCall">Whether this method is being called by a TFunction.</param>
        /// <returns>
        /// TNil if the method is not being called by a TFunction, otherwise the resulting value of the statement.
        /// </returns>
        public TType Interpret(string statement, bool isFunctionCall = false)
        {
            statement = statement.Trim();

            // Halt the interpreter execution if the command is 'quit'
            if (statement == "quit")
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

            // Ignore comments and empty strings
            int commentIndex;
            if ((commentIndex = statement.IndexOf("//")) >= 0) statement = statement.Remove(commentIndex);
            if (statement == "") return TNil.Instance;

            // Convert the statement into a Group, outputting any errors
            TException exception;
            Group group = SplitIntoSymbols(statement, out exception);
            if (exception != null)
            {
                System.Console.WriteLine(exception.ToCSString());
                return TNil.Instance;
            }

            // Parse the group and output the return value
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

            // Leave a gap if running from the CLI to keep things readable
            if ((CurrentBlock == null) && !RunningFromFile) System.Console.WriteLine();

            return TNil.Instance;
        }

        /// <summary>
        /// Interprets Toast code that has been split into symbols, contained in a Interpreter.Group
        /// </summary>
        /// <param name="group">The group to Parse.</param>
        /// <returns>The result of the statement or expression contained in the group.</returns>
        public TType ParseGroup(Group group)
        {
            if (group.Count == 0) return new TArgumentList();

            // Parse any keywords first
            string firstSymbolStr = group[0] as string;
            if (firstSymbolStr != null)
            {
                if (firstSymbolStr == TType.DIRECTIVE_CHARACTER.ToString()) return ParseDirectiveGroup(group);

                switch (firstSymbolStr)
                {
                    case "let": return ParseAssignmentGroup(group);
                    case "if": return ParseIfGroup(group);
                    case "while": return ParseWhileGroup(group);
                    case "begin": return ParseBegin();
                    case "end": return new TException(this, "Unexpected keyword 'end'");
                    case "else": return new TException(this, "Unexpected keyword 'else'");
                    case "for": break;
                }
            }

            // Recursively iterate through the groups
            for (int i = 0; i < group.Count; ++i)
            {
                Group nextGroup = group[i] as Group;
                if (nextGroup != null)
                {
                    TType value = ParseGroup(nextGroup);
                    if (value is TException) return value;
                    group[i] = value;
                }

                // Attempt a function call
                TType argument = group[i] as TType;
                if ((argument != null) && (i - 1 >= 0))
                {
                    bool functionCalled = false;
                    TType functionReturnValue = null;

                    // If the symbol before the argument is a TFunction call it
                    TType value = TType.Parse(this, group[i - 1]);
                    TFunction function = value as TFunction;
                    if (function != null)
                    {
                        functionReturnValue = function.Call(this, argument);
                        functionCalled = true;
                    }
                    else
                    {
                        // If it's a TBlock, execute it
                        TBlock block = value as TBlock;
                        if (block != null)
                        {
                            bool exitFromFunction;
                            functionReturnValue = block.Execute(this, out exitFromFunction);
                            if (exitFromFunction) return functionReturnValue;
                            functionCalled = true;
                        }
                        else
                        {
                            // If it's a TVariable, and it's value is a TFunction or TBlock, execute it
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
                        // Replace the function and argument in the group with the returned value
                        if (functionReturnValue is TException) return functionReturnValue;
                        group[i - 1] = functionReturnValue;
                        group.RemoveAt(i);
                        --i;
                    }
                }
            }

            TException exception = ParseReferencesOfGroup(group);
            if (exception != null) return exception;

            exception = ParseModulusBracketsOfGroup(group);
            if (exception != null) return exception;

            exception = ParseBidmasOfGroup(group);
            if (exception != null) return exception;
            
            exception = ParseComparisonsOfGroup(group);
            if (exception != null) return exception;

            exception = ParseLogicalOperatorsOfGroup(group);
            if (exception != null) return exception;

            exception = ParseCommasOfGroup(group);
            if (exception != null) return exception;

            if (group.Count == 0) return TNil.Instance;
            else if (group.Count == 1) return TType.Parse(this, group[0]);
            else return new TException(this, "Statement could not be evaluated completely");
        }

        /// <summary>
        /// Parses an assignment operation.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>The assigned variable or function on success, otherwise a TException.</returns>
        TType ParseAssignmentGroup(Group group)
        {
            /* BNF for assignment:
             *      <assignment>       ::= 'let' ( <variable-assignment> | <function-declaration> )
             *      <var-assignment>   ::= <identifier> '=' <expression>
             *      <func-declaration> ::= <identifier> '(' { <parameters> } ')' '=' <statements>
             *      <parameters>       ::= <identifier> { ',' <identifier> }*
             *      <statements>       ::= <block> | <statement>
             *      <block>            ::= <new-line> (<statement> <new-line>)* 'end'
             * You can probably guess what <identifier>, <statement> and <new-line> are
             * The 'let' is assumed to have already been checked for
             */
            int equalsIndex = group.IndexOf("=");
            if (equalsIndex < 0) return new TException(this, "Variable or function could not be assigned a value");

            // Could be assigning a dereferenced variable
            TException exception = ParseReferencesOfGroup(group, equalsIndex);
            if (exception != null) return exception;

            if (group.Count == 1) return new TException(this, "Could not assign variable", "no variable name given");

            string variableName = group[1] as string;
            TVariable existingVariable = null;

            if (variableName == null)
            {
                exception = new TException(this, "Could not assign variable", "invalid variable name given");

                // Check if an existing variable or function is being assigned
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
            if (assignmentOperator == null) // Now we assume that we're dealing with a function declaration
            {
                Group paramGroup = group[2] as Group;
                if (paramGroup == null) // The user probably just wanted to assign a variable but made a typo
                    return new TException(this, "Variable could not be assigned a value",
                        "value to assign to variable must be given");

                // Get the identifiers of all the parameters within the brackets, keeping strict watch on comma usage
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

                exception = new TException(this, "Function could not be given a body", "function body must be given");
                if (group.Count == 3) return exception;
                assignmentOperator = group[3] as string;
                if (assignmentOperator == null) return exception;
                else if (assignmentOperator != "=") return exception;

                TFunction function;
                if (group.Count == 4) // statement is just 'let <i><params> =', so get a block
                {
                    TBlock block = new TBlock(this, out exception, false);
                    if (exception != null) return exception;
                    function = new TFunction(variableName ?? existingVariable.Identifier, block,
                        paramList.ParameterNames, null);
                }
                else // Create a single line function
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
            { // Assume that we're dealing with a variable assigment
                exception = new TException(this, "Variable could not be assigned a value",
                    "value to assign to variable must be given");

                if (assignmentOperator != "=") return exception;
                if (group.Count == 3) return exception;

                // Parse the expression on the right hand side of the assigment operator, and if the result is a
                // TVariable, then get it's value (we don't want to assign the TVariable itself to the new TVariable)
                Group valueGroup = new Group(null);
                valueGroup.AddRange(group.GetRange(3, group.Count - 3));
                TType value = ParseGroup(valueGroup);
                if (value is TException) return value;

                TVariable variable = value as TVariable;
                if (variable != null) value = variable.Value;

                // Make sure we don't get any circular references; they cause stack overflows when outputting them
                variable = existingVariable ?? Stack.FindVariable(variableName);
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

                if (variable == null) // If the variable doesn't exist already, add it to the stack variable dictionary
                {
                    variable = new TVariable(variableName, value);
                    Stack.AddVariable(variable);
                }
                else variable.Value = value;

                return variable;
            }
        }

        /// <summary>
        /// Parses an if statement.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>
        /// If a statement or block of code is executed, then the result of that code is returned, otherwise the
        /// boolean result of the logical expression is returned. A TException is returned when there is an error.
        /// </returns>
        TType ParseIfGroup(Group group)
        {
            /* BNF for if statement:
             *      <if-statement> ::= 'if' <condition> ',' ( <statements> | <block-no-end> 'else' <statements> )
             *      <block-no-end> ::= <new-line> (<statement> <new-line>)*
             * The 'if' is assumed to have already been checked for
             */
            if (group.Count < 2)
                return new TException(this, "Statement could not be evaluated",
                    "if statement must be given a condition");

            int commaIndex = group.IndexOf(",");
            if (commaIndex < 0)
                return new TException(this, "if statment invalid", "comma required after condition");

            Group conditionGroup = new Group(null);
            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));
            TType value = ParseGroup(conditionGroup);
            if (value is TException) return value;

            TBoolean result = value as TBoolean;
            if (result == null) return new TException(this, "Condition does not evaluate to a boolean value",
                "yes or no");

            if (group.Count > commaIndex + 1) // If there is no block after the 'if' <condition> ','
            {
                // If there is an else and it's at the end of the line, get a block for the else
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
                    // Execute the code between the comma and the end of the statement or the else (if any).
                    // If there isn't anything to execute, then a block is created (near the end of this method)
                    Group statementGroup = new Group(null);
                    if (elseIndex < 0)
                        statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
                    else
                        statementGroup.AddRange(group.GetRange(commaIndex + 1, elseIndex - (commaIndex + 1)));

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

            { // This code is executed if there is a block after the 'if' <condition> ',' instead of a single statement
                TException exception;
                TBlock block = new TBlock(this, out exception, true);
                if (exception != null) return exception;

                bool exitFromFunction;
                if (result.Value) return block.Execute(this, out exitFromFunction, true);
                if (block.HasElse()) return block.Execute(this, out exitFromFunction, false);
            }

            return result;
        }

        /// <summary>
        /// Parses a while loop statement.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>TNil on success, otherwise a TException.</returns>
        TType ParseWhileGroup(Group group)
        {
            /* BNF for while loop:
             *      <while-loop> ::= 'while' <condition> ',' ( <statement> | <block> )
             * The 'while' is assumed to have already been checked for
             */
            if (group.Count == 1) return new TException(this, "Statement could not be evaluated",
                "while loop must be given a condition");

            int commaIndex = group.IndexOf(",");
            if (commaIndex < 0) return new TException(this, "while loop invalid", "comma required after condition");

            if (group.Count == 1)
                return new TException(this,
                    "Statement could not be evaluated", "while loop must be given a condition");

            Group conditionGroup = new Group(null);
            conditionGroup.AddRange(group.GetRange(1, commaIndex - 1));

            Group statementGroup = null;
            TBlock block = null;
            if (group.Count > commaIndex + 1) // If there is a statement after the comma, use that statement
            {
                statementGroup = new Group(null);
                statementGroup.AddRange(group.GetRange(commaIndex + 1, group.Count - (commaIndex + 1)));
            }
            else // Otherwise get a block
            {
                TException exception;
                block = new TBlock(this, out exception, false);
                if (exception != null) return exception;
            }

            while (true)
            {
                // Parse the condition, and if it's true run the block or single statement, otherwise return MNil
                TType value = ParseGroup((Group)conditionGroup.Clone());
                if (value is TException) return value;

                TBoolean result = value as TBoolean;
                if (result == null)
                    return new TException(this, "Condition does not evaluate to a boolean value", "yes or no");

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

        /// <summary>
        /// Parses a 'begin', indicating the start of a block of code.
        /// </summary>
        /// <returns>A TBlock on success, otherwise a TException.</returns>
        TType ParseBegin()
        {
            TException exception;
            TType block = new TBlock(this, out exception, false);
            return exception ?? block;
        }

        /// <summary>
        /// Parses and puts into effect an interpreter directive.
        /// </summary>
        /// <param name="group">The group containing the directive.</param>
        /// <returns>TNil on success, otherwise a TException.</returns>
        TType ParseDirectiveGroup(Group group)
        {
            if (group.Count < 2) return new TException(this, "No directive given");

            string directive = group[1] as string;
            if (directive == null) return new TException(this, "No directive given");

            switch (directive)
            {
                case "STRICT": // Toggles strict mode. Usage: '#STRICT yes', '#STRICT no' or '#STRICT <condition>'
                    if (group.Count > 2)
                    {
                        Group conditionGroup = new Group(null);
                        conditionGroup.AddRange(group.GetRange(2, group.Count - 2));
                        TType value = ParseGroup(conditionGroup);
                        if (value is TException) return value;

                        TBoolean result = value as TBoolean;
                        if (result == null) return new TException(this, "Directive 'STRICT' could not be used",
                            "invalid argument; use yes, no or a condition");

                        Strict = result.Value;
                        if (Strict) System.Console.WriteLine("Interpreter running in strict mode");
                        else System.Console.WriteLine("Interpreter not running in strict mode");

                        return TNil.Instance;
                    }
                    else return new TException(this, "Directive 'STRICT' could not be used",
                        "invalid argument (none given); use yes, no or a condition");
            }

            return new TException(this, "Could not use directive", "directive '" + directive + "' not recognised");
        }

        /// <summary>
        /// Parses a group, searching for any reference and dereference operators. Operators and operands are
        /// replaced with the reference/dereferenced value.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <param name="limit">The index of the group to parse up to. Used by the ParseAssignmentGroup method.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseReferencesOfGroup(Group group, int limit = -1)
        {
            int limitToUse = limit < 0 ? group.Count - 1 : limit;
            int index;

            while (true)
            {
                // Search from right to left for the first reference or dereference character
                int refIndex = group.LastIndexOf(TType.REFERENCE_CHARACTER.ToString(), limitToUse),
                    derefIndex = group.LastIndexOf(TType.DEREFERENCE_CHARACTER.ToString(), limitToUse);

                char character; // Used so we know what operation we're carrying out later
                if (refIndex < 0)
                {
                    index = derefIndex;
                    character = TType.DEREFERENCE_CHARACTER;
                }
                else if (derefIndex < 0)
                {
                    index = refIndex;
                    character = TType.REFERENCE_CHARACTER;
                }
                else
                {
                    if (refIndex > derefIndex)
                    {
                        index = refIndex;
                        character = TType.REFERENCE_CHARACTER;
                    }
                    else
                    {
                        index = derefIndex;
                        character = TType.DEREFERENCE_CHARACTER;
                    }
                }
                if (index < 0) break;

                if (index + 1 > group.Count)
                    return new TException(this, "Invalid expression term '" + character + "' at end of statement");

                // If the operand is not a variable, return a TException (can't dereference values)
                TType variable = TType.Parse(this, group[index + 1]);
                if (variable is TException) return variable as TException;
                if (!(variable is TVariable))
                {
                    if (character == TType.REFERENCE_CHARACTER)
                        return new TException(this,
                            "Attempted creation of reference to value", "expected variable identifier");
                    else
                        return new TException(this, "Attempted dereference of value", "expected variable identifier");
                }

                if (character == TType.REFERENCE_CHARACTER) group[index] = new TVariable("reference", variable);
                else
                {
                    group[index] = ((TVariable)variable).Value;
                    if (!(group[index] is TVariable) && !(group[index] is TFunction))
                        return new TException(this,
                            "Dereference of value type variable", "expected reference");
                }
                group.RemoveAt(index + 1);
                limitToUse = limit < 0 ? group.Count - 1 : limit;
            }

            return null;
        }

        /// <summary>
        /// Parses a group, searching for any modulus brackets and replacing the modulus brackets and their contents
        /// with the absolute values.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseModulusBracketsOfGroup(Group group)
        {
            int index;
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
                if (value is TException) return value as TException;

                TType result = Operations.Math.Modulus(this, value);
                if (result == null) return new TException(this, "Modulus operation failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index] = result;
                group.RemoveRange(index + 1, 2);
            }

            return null;
        }

        /// <summary>
        /// Parses all of the arithmetic in the group, according to BIDMAS (although ignoring Brackets, because
        /// they're taken care of elsewhere), replacing the operands and operators with the results.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseBidmasOfGroup(Group group)
        {
            // For every while loop, convert the values either side of the operator to the relevant TType, and attempt
            // to perform the operation on them. Then replace the operands and operator in the group with the result

            int index;
            while ((index = group.IndexOf("^")) >= 0) // Indicies
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '^'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Math.Pow(this, a, b);
                if (result == null) return new TException(this, "Exponentiation operation failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while (true) // Division and Multiplication
            {
                // Find out which operator comes first in order to parse the expression from left to right
                int divIndex = group.IndexOf("/"), mulIndex = group.IndexOf("*");
                if (divIndex < 0) index = mulIndex;
                else if (mulIndex < 0) index = divIndex;
                else index = divIndex < mulIndex ? divIndex : mulIndex;
                if (index < 0) break;

                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '" + group[index] + "'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result;
                if ((string)group[index] == "/")
                {
                    result = Operations.Math.Divide(this, a, b);
                    if (result == null) return new TException(this, "Division operation failed", "reason unknown");
                }
                else
                {
                    result = Operations.Math.Multiply(this, a, b);
                    if (result == null)
                        return new TException(this, "Multiplication operation failed", "reason unknown");
                }

                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while (true) // Addition and Subtraction
            {
                int addIndex = group.IndexOf("+"), subIndex = group.IndexOf("-");
                if (addIndex < 0) index = subIndex;
                else if (subIndex < 0) index = addIndex;
                else index = addIndex < subIndex ? addIndex : subIndex;
                if (index < 0) break;

                char operatorChar = ((string)group[index])[0];

                if (index - 1 < 0)
                { // There's no number before the operator, so perform a unary operation
                    TException exception = ParseUnaryArtitmetic(group, index, operatorChar);
                    if (exception != null) return exception;
                    continue;
                }
                else if (index + 1 >= group.Count)
                    return new TException(this, "Invalid expression term '" + group[index] + "'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException)
                {
                    string possibleOperator = group[index - 1] as string;
                    if (possibleOperator != null)
                    {
                        if (RESERVED_SYMBOLS.Contains(possibleOperator))
                        {
                            TException exception = ParseUnaryArtitmetic(group, index, operatorChar);
                            if (exception != null) return exception;
                            continue;
                        }
                    }
                    return a as TException;
                }
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result;
                if (operatorChar == '+')
                {
                    result = Operations.Math.Add(this, a, b);
                    if (result == null) return new TException(this, "Addition operation failed", "reason unknown");
                }
                else
                {
                    result = Operations.Math.Subtract(this, a, b);
                    if (result == null) return new TException(this, "Subtraction operation failed", "reason unknown");
                }

                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            return null;
        }

        TException ParseUnaryArtitmetic(Group group, int index, char operatorChar)
        {
            if (index + 1 >= group.Count)
                return new TException(this, "Invalid expression term '" + group[index] + "'");

            // Try and get a TNumber value out of the operand
            TType operand = TType.Parse(this, group[index + 1]);
            if (operand is TException) return operand as TException;

            TNumber number = operand as TNumber;
            if (number == null)
            {
                TVariable variable = operand as TVariable;
                if (variable == null)
                    return new TException(this, "Failed to perform unary arithmetic operation",
                        "only numbers can be used");

                number = variable.Value as TNumber;
                if (number == null)
                    return new TException(this, "Failed to perform unary arithmetic operation",
                        "only numbers can be used");
            }

            if (operatorChar == '-') group[index] = number.ToNegative();
            else group[index] = number;
            group.RemoveAt(index + 1);

            return null;
        }

        /// <summary>
        /// Parses the logical comparisons of the group. Replaces the operators and operands in the group with their
        /// results.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseComparisonsOfGroup(Group group)
        {
            int index;
            while ((index = group.IndexOf("=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Equal(this, a, b, true);
                if (result == null) return new TException(this, "Comparison operation failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("~=")) >= 0) // Approximately equal (by rounding or within ~0.5)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '~='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Equal(this, a, b, false);
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("/=")) >= 0) // Not equal
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '/='");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.NotEqual(this, a, b);
                if (result == null) return new TException(this, "Comparison operation failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Math.Inequality(this, a, b, "<");
                if (result == null) return new TException(this, "Less than comparison failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Math.Inequality(this, a, b, ">");
                if (result == null)
                    return new TException(this, "Greater than comparison failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("<=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Math.Inequality(this, a, b, "<=");
                if (result == null)
                    return new TException(this, "Less than or equal comparison failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf(">=")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term '<'");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TType result = Operations.Math.Inequality(this, a, b, ">=");
                if (result == null)
                    return new TException(this, "Greater than or equal comparison failed", "reason unknown");
                if (result is TException) return result as TException;
                group[index - 1] = result;
                group.RemoveRange(index, 2);
            }

            return null;
        }

        /// <summary>
        /// Parses the logical operators of the group. Replaces the operators and operands with their results.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseLogicalOperatorsOfGroup(Group group)
        {
            int index;
            while ((index = group.IndexOf("and")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TBoolean aBool = a as TBoolean;
                if (aBool == null)
                    return new TException(this, "Left hand side of expression must be a boolean value", "yes or no");
                TBoolean bBool = b as TBoolean;
                if (bBool == null)
                    return new TException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new TBoolean(aBool.Value && bBool.Value);
                group.RemoveRange(index, 2);
            }

            while ((index = group.IndexOf("or")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

                TBoolean aBool = a as TBoolean;
                if (aBool == null)
                    return new TException(this, "Left hand side of expression must be a boolean value", "yes or no");
                TBoolean bBool = b as TBoolean;
                if (bBool == null)
                    return new TException(this, "Right hand side of expression must be a boolean value", "yes or no");

                group[index - 1] = new TBoolean(aBool.Value || bBool.Value);
                group.RemoveRange(index, 2);
            }

            return null;
        }

        /// <summary>
        /// Parses the commas in the group, converting a comma separated list into a TArgumentList, and replacing the
        /// list in the group with it.
        /// </summary>
        /// <param name="group">The group to parse.</param>
        /// <returns>A TException on failure, otherwise null.</returns>
        TException ParseCommasOfGroup(Group group)
        {
            int index;
            while ((index = group.IndexOf(",")) >= 0)
            {
                if ((index - 1 < 0) || (index + 1 >= group.Count))
                    return new TException(this, "Invalid expression term ','");

                TType a = TType.Parse(this, group[index - 1]);
                if (a is TException) return a as TException;
                TType b = TType.Parse(this, group[index + 1]);
                if (b is TException) return b as TException;

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

            return null;
        }
    }
}
