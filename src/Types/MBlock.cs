using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types.Singletons;

namespace MathsLanguage.Types
{
    class MBlock : MType
    {
        List<string> statements;
        int otherwiseLocation, currentLine;
        bool otherwiseAllowed;

        public MBlock(Interpreter interpreter, out MException exception, bool otherwiseAllowed)
        {
            statements = new List<string>();
            otherwiseLocation = -1;
            this.otherwiseAllowed = otherwiseAllowed;
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
                    exception = new MException(interpreter, "Having more than one begin on a line is forbidden");
                    break;
                }
                else if (beginCount == 1) ++blockLevel;

                keywordName = "end";
                int endCount = keywordQuery.Count();
                if (endCount > 1)
                {
                    exception = new MException(interpreter, "Having more than one end on a line is forbidden");
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
                int otherwiseIndex = -1;
                while ((otherwiseIndex = symbolGroup.IndexOf("otherwise", otherwiseIndex + 1)) >= 0)
                {
                    if (otherwiseIndex == 0)
                    {
                        if (blockLevel == 0)
                        {
                            if ((otherwiseLocation < 0) && otherwiseAllowed)
                            {
                                otherwiseLocation = statements.Count;
                                if (otherwiseIndex < symbolGroup.Count - 1)
                                {
                                    statements.Add("otherwise");
                                    statements.Add(statement.Substring(10, statement.Length - 10));
                                    breakNow = true;
                                }
                            }
                            else
                            {
                                exception = new MException(interpreter, "Unexpected keyword 'otherwise'", "otherwise already used");
                                statements.Clear();
                                breakNow = true;
                                break;
                            }
                        }
                        else if (otherwiseIndex < symbolGroup.Count - 1) --blockLevel;
                    }
                    else if (symbolGroup.LastIndexOf("if", otherwiseIndex) >= 0)
                    {
                        if (otherwiseIndex == symbolGroup.Count - 1) ++blockLevel;
                    }
                }
                if (breakNow) break;
                
                statements.Add(statement);
            }
        }

        public bool HasOtherwise()
        {
            return (otherwiseLocation >= 0);
        }

        public string GetInput()
        {
            if (++currentLine >= statements.Count) return "";
            return statements[currentLine];
        }

        public MType Execute(Interpreter interpreter, bool beforeOtherwise = true)
        {
            if (!beforeOtherwise && (otherwiseLocation < 0)) return new MException(interpreter, "No otherwise statement found");

            int start, end;

            if (otherwiseAllowed)
            {
                if (beforeOtherwise)
                {
                    start = 0;
                    end = (otherwiseLocation > 0 ? otherwiseLocation : statements.Count) - 1;
                }
                else
                {
                    start = otherwiseLocation + 1;
                    end = statements.Count - 1;
                }
            }
            else
            {
                start = 0;
                end = statements.Count - 1;
            }

            MBlock previousCurrentBlock = interpreter.CurrentBlock;
            interpreter.CurrentBlock = this;

            MType returnValue = null;
            for (currentLine = start; currentLine <= end; ++currentLine)
            {
                returnValue = interpreter.Interpret(statements[currentLine], true);
                if ((returnValue is MException)  || (returnValue is MBreak))
                {
                    interpreter.CurrentBlock = previousCurrentBlock;
                    return returnValue;
                }
            }

            interpreter.CurrentBlock = previousCurrentBlock;

            return returnValue ?? MNil.Instance;
        }

        public MType ExecuteLine(Interpreter interpreter)
        {
            // Needs to be done this way so block doesn't return nil when called in function
            if (currentLine < 0) currentLine = 0;
            else if (currentLine >= statements.Count) return MNil.Instance;
            MType returnValue = interpreter.Interpret(statements[currentLine], true);
            ++currentLine;
            return returnValue;
        }

        public void ResetLine()
        {
            currentLine = -1;
        }

        public bool EndOfBlock { get { return currentLine >= statements.Count; } }

        public override string TypeName { get { return M_BLOCK_TYPENAME; } }

        public override string ToCSString()
        {
            return "block";
        }
    }
}
