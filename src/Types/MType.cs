using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types.Singletons;

namespace MathsLanguage.Types
{
    abstract class MType
    {
        public abstract string TypeName { get; }

        public const string M_INTEGER_TYPENAME = "integer";
        public const string M_REAL_TYPENAME = "real";
        public const string M_BOOLEAN_TYPENAME = "boolean";
        public const string M_STRING_TYPENAME = "string";
        public const string M_VARIABLE_TYPENAME = "variable";
        public const string M_EXCEPTION_TYPENAME = "exception";
        public const string M_ARGUMENTLIST_TYPENAME = "argumentList";
        public const string M_PARAMETERLIST_TYPENAME = "parameterList";
        public const string M_BLOCK_TYPENAME = "block";

        public const string M_NIL_TYPENAME = "nil";
        public const string M_BREAK_TYPENAME = "break";

        public const char DIRECTIVE_CHARACTER = '#';
        public const char REFERENCE_CHARACTER = '@';
        public const char DEREFERENCE_CHARACTER = '~';
        public const char STRING_CHARACTER = '"';
        public const char MODULUS_CHARACTER = '|';

        public abstract string ToCSString();
        public override string ToString()
        {
            return ToCSString();
        }

        public static MType Parse(Interpreter interpreter, object obj)
        {
            if (obj is string)
            {
                string str = ((string)obj).Trim();
                if (str == "") return MNil.Instance;

                if ((str == "yes") || (str == "no")) return new MBoolean(str);
                if (str == "nil") return MNil.Instance;
                if (str == "break") return MBreak.Instance;

                {
                    long result;
                    if (long.TryParse(str, out result)) return new MInteger(result);
                }
                {
                    double result;
                    if (double.TryParse(str, out result)) return new MReal(result);
                }
                {
                    if ((str.First() == STRING_CHARACTER) && (str.Last() == STRING_CHARACTER))
                        return new MString(str.Substring(1, str.Length - 2));
                }
                {
                    MType result = MFunction.GetFunction(str);
                    if (result == null)
                    {
                        result = interpreter.Stack.FindVariable(str);
                        if (result == null) return new MException(interpreter, "Variable or function with identifier '" + str + "' not found");
                    }
                    return result;
                }
            }
            else if (obj is MType) return (MType)obj;
            else if (obj is Interpreter.Group) return interpreter.ParseGroup(obj as Interpreter.Group);

            return null;
        }
    }
}
