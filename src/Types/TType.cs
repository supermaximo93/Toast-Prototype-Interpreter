using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types.Singletons;

namespace Toast.Types
{
    /// <summary>
    /// The base class for all Toast types.
    /// </summary>
    abstract class TType
    {
        public abstract string TypeName { get; }

        public const string T_INTEGER_TYPENAME = "integer";
        public const string T_REAL_TYPENAME = "real";
        public const string T_FRACTION_TYPENAME = "fraction";

        public const string T_BOOLEAN_TYPENAME = "boolean";
        public const string T_STRING_TYPENAME = "string";
        public const string T_VARIABLE_TYPENAME = "variable";
        public const string T_EXCEPTION_TYPENAME = "exception";
        public const string T_ARGUMENTLIST_TYPENAME = "argumentList";
        public const string T_PARAMETERLIST_TYPENAME = "parameterList";
        public const string T_BLOCK_TYPENAME = "block";

        public const string T_NIL_TYPENAME = "nil";
        public const string T_BREAK_TYPENAME = "break";
        public const string T_INFINITY_TYPENAME = "infinity";

        public const char DIRECTIVE_CHARACTER = '#';
        public const char REFERENCE_CHARACTER = '@';
        public const char DEREFERENCE_CHARACTER = '~';
        public const char STRING_CHARACTER = '"';
        public const char MODULUS_CHARACTER = '|';

        public abstract string ToCSString(); // i.e. To C Sharp String
        public override string ToString()
        {
            return ToCSString();
        }

        /// <summary>
        /// Attempts to convert an object into a TType.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="obj">The object that is to be converted into a TType derivative.</param>
        /// <returns>An object of base class TType. If the operation was unsuccessful, null is returned.</returns>
        public static TType Parse(Interpreter interpreter, object obj)
        {
            if (obj is string)
            {
                string str = ((string)obj).Trim();
                if (str == "") return TNil.Instance;

                if ((str == "yes") || (str == "no")) return new TBoolean(str);
                if (str == "nil") return TNil.Instance;
                if (str == "break") return TBreak.Instance;

                {
                    long result;
                    if (long.TryParse(str, out result)) return new TInteger(result);
                }
                {
                    double result;
                    if (double.TryParse(str, out result)) return new TReal(result);
                }
                {
                    if ((str.First() == STRING_CHARACTER) && (str.Last() == STRING_CHARACTER))
                        return new TString(str.Substring(1, str.Length - 2));
                }
                {
                    TType result = TFunction.GetFunction(str);
                    if (result == null)
                    {
                        result = interpreter.Stack.FindVariable(str);
                        if (result == null)
                            return new TException(interpreter,
                                "Variable or function with identifier '" + str + "' not found");
                    }
                    return result;
                }
            }
            else if (obj is TType) return (TType)obj;
            else if (obj is Interpreter.Group) return interpreter.ParseGroup(obj as Interpreter.Group);

            return null;
        }
    }
}
