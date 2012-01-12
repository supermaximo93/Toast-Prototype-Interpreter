using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

                if (statement.IndexOf("begin") >= 0) ++blockLevel;
                else if (statement.IndexOf("when") >= 0)
                {
                    int commaIndex = statement.IndexOf(",");
                    if (commaIndex == statement.Length - 1) ++blockLevel;
                }
                else if (statement.IndexOf("end") >= 0)
                {
                    if (blockLevel == 0) break;
                    else --blockLevel;
                }

                int otherwiseIndex;
                if (((otherwiseIndex = statement.IndexOf("otherwise")) >= 0) && (blockLevel == 0)) // otherwise with when possible
                {
                    if ((otherwiseLocation < 0) && otherwiseAllowed)
                    {
                        otherwiseLocation = statements.Count;
                        if (otherwiseIndex + 9 < statement.Length - 1)
                        {
                            statements.Add("otherwise");
                            statement = statement.Remove(0, 9);

                            if (blockLevel == 0) break;
                            else --blockLevel;
                        }
                    }
                    else
                    {
                        if (otherwiseIndex == 0)
                        {
                            exception = new MException(interpreter, "Unexpected keyword 'otherwise'", "otherwise already used");
                            statements.Clear();
                            break;
                        }
                    }
                }
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
                if (returnValue is MException)
                {
                    interpreter.CurrentBlock = previousCurrentBlock;
                    return returnValue;
                }
            }

            interpreter.CurrentBlock = previousCurrentBlock;

            return returnValue ?? new MNil();
        }

        public MType ExecuteLine(Interpreter interpreter)
        {
            // Needs to be done this way so block doesn't return nil when called in function
            if (currentLine < 0) currentLine = 0;
            else if (currentLine >= statements.Count) return new MNil();
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
