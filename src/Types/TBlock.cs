using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types.Singletons;

namespace Toast.Types
{
    class TBlock : TType
    {
        List<string> statements;
        int elseLocation, currentLine;
        bool elseAllowed;

        public TBlock(Interpreter interpreter, out TException exception, bool elseAllowed)
        {
            statements = new List<string>();
            elseLocation = -1;
            this.elseAllowed = elseAllowed;
            exception = null;
            currentLine = -1;

            int blockLevel = 0;
            while (true)
            {
                string statement = interpreter.GetInput().Trim();

                Interpreter.Group symbolGroup = interpreter.SplitIntoSymbols(statement, out exception);
                if (exception != null)
                {
                    statements.Clear();
                    break;
                }

                string keywordName = "";
                var keywordQuery =
                    from object item in symbolGroup
                    where (item is string) && ((string)item == keywordName)
                    select item;

                keywordName = "begin";
                int beginCount = keywordQuery.Count();
                if (beginCount > 1)
                {
                    exception = new TException(interpreter, "Having more than one begin on a line is forbidden");
                    break;
                }
                else if (beginCount == 1) ++blockLevel;

                keywordName = "end";
                int endCount = keywordQuery.Count();
                if (endCount > 1)
                {
                    exception = new TException(interpreter, "Having more than one end on a line is forbidden");
                    break;
                }
                else if (endCount == 1)
                {
                    if (blockLevel == 0) break;
                    else --blockLevel;
                }

                if (symbolGroup.IndexOf("if") >= 0)
                {
                    string commaStr = symbolGroup[symbolGroup.Count - 1] as string;
                    if (commaStr != null)
                    {
                        if (commaStr == ",") ++blockLevel;
                    }
                }

                if (symbolGroup.IndexOf("while") >= 0)
                {
                    string commaStr = symbolGroup[symbolGroup.Count - 1] as string;
                    if (commaStr != null)
                    {
                        if (commaStr == ",") ++blockLevel;
                    }
                }

                bool breakNow = false; // For a double break
                int elseIndex = -1;
                while ((elseIndex = symbolGroup.IndexOf("else", elseIndex + 1)) >= 0)
                {
                    if (elseIndex == 0)
                    {
                        if (blockLevel == 0)
                        {
                            if ((elseLocation < 0) && elseAllowed)
                            {
                                elseLocation = statements.Count;
                                if (elseIndex < symbolGroup.Count - 1)
                                {
                                    statements.Add("else");
                                    statements.Add(statement.Substring(10, statement.Length - 10));
                                    breakNow = true;
                                }
                            }
                            else
                            {
                                exception = new TException(interpreter, "Unexpected keyword 'else'", "else already used");
                                statements.Clear();
                                breakNow = true;
                                break;
                            }
                        }
                        else if (elseIndex < symbolGroup.Count - 1) --blockLevel;
                    }
                    else if (symbolGroup.LastIndexOf("if", elseIndex) >= 0)
                    {
                        if (elseIndex == symbolGroup.Count - 1) ++blockLevel;
                    }
                }
                if (breakNow) break;
                
                statements.Add(statement);
            }
        }

        public bool HasOtherwise()
        {
            return (elseLocation >= 0);
        }

        public string GetInput()
        {
            if (++currentLine >= statements.Count) return "";
            return statements[currentLine];
        }

        public TType Execute(Interpreter interpreter, out bool exitFromFunction, bool beforeOtherwise = true, bool inLoop = false)
        {
            exitFromFunction = false;
            if (!beforeOtherwise && (elseLocation < 0)) return new TException(interpreter, "No else statement found");

            int start, end;

            if (elseAllowed)
            {
                if (beforeOtherwise)
                {
                    start = 0;
                    end = (elseLocation > 0 ? elseLocation : statements.Count) - 1;
                }
                else
                {
                    start = elseLocation + 1;
                    end = statements.Count - 1;
                }
            }
            else
            {
                start = 0;
                end = statements.Count - 1;
            }

            int stackLevel = interpreter.Stack.Level;
            TBlock previousCurrentBlock = interpreter.CurrentBlock;
            interpreter.CurrentBlock = this;

            TType returnValue = null;
            for (currentLine = start; currentLine <= end; ++currentLine)
            {
                returnValue = interpreter.Interpret(statements[currentLine], true);

                if (interpreter.Stack.Level < stackLevel)
                {
                    exitFromFunction = true;
                    break;
                }

                if ((returnValue is TException) || (returnValue is TBreak)) break;
            }

            interpreter.CurrentBlock = previousCurrentBlock;

            return returnValue ?? TNil.Instance;
        }

        public TType ExecuteLine(Interpreter interpreter)
        {
            // Needs to be done this way so block doesn't return nil when called in function
            if (currentLine < 0) currentLine = 0;
            else if (currentLine >= statements.Count) return TNil.Instance;
            TType returnValue = interpreter.Interpret(statements[currentLine], true);
            ++currentLine;
            return returnValue;
        }

        public void ResetLine()
        {
            currentLine = -1;
        }

        public bool EndOfBlock { get { return currentLine >= statements.Count; } }

        public override string TypeName { get { return T_BLOCK_TYPENAME; } }

        public override string ToCSString()
        {
            return "block";
        }
    }
}
