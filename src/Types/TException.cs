using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType that is used as a return value when errors occur. When methods see an instance of this class returned,
    /// it gets passed through to the top level of the interpreter and the error message is shown to the user.
    /// </summary>
    class TException : TType
    {
        public string Error { get; set; }
        public string Hint { get; set; }

        readonly bool fatal;
        readonly int line;

        public TException(Interpreter interpreter, string error, string hint = "")
        {
            Error = error;
            Hint = hint;
            fatal = interpreter.Strict;
            line = interpreter.CurrentLine;

            if (fatal) interpreter.Kill();
        }

        public override string TypeName { get { return T_EXCEPTION_TYPENAME; } }

        public override string ToCSString()
        {
            StringBuilder str = new StringBuilder("*** ");
            str.Append(fatal ? "Fatal error" : "Warning")
                .Append("! ")
                .Append(Error)
                .Append(" on line ")
                .Append(line.ToString());
            if (Hint.Length > 0) str.Append(" (").Append(Hint).Append(")");
            return str.ToString();
        }
    }
}
