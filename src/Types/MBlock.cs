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

        public MBlock(Interpreter interpreter, out MException exception)
        {
            statements = new List<string>();
            otherwiseLocation = -1;
            exception = null;
            currentLine = -1;

            int blockLevel = 0;
            while (true)
            {
                string statement = interpreter.GetInput().Trim();

                if (statement.IndexOf("begin") >= 0) ++blockLevel;
                else if (statement.IndexOf("end") >= 0)
                {
                    if (blockLevel == 0) break;
                    else --blockLevel;
                }
                else if ((statement == "otherwise") && (blockLevel == 0))
                {
                    if (otherwiseLocation < 0) otherwiseLocation = statements.Count;
                    else
                    {
                        exception = new MException(interpreter, "Unexpected keyword 'otherwise'", "otherwise already used");
                        statements.Clear();
                        break;
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

            MType returnValue = null;
            int start, end;
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

            MBlock previousCurrentBlock = interpreter.CurrentBlock;
            interpreter.CurrentBlock = this;

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

        public override string TypeName { get { return M_BLOCK_TYPENAME; } }

        public override string ToCSString()
        {
            return "block";
        }
    }
}
