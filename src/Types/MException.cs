using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MException : MType
    {
        private string error;
        public string Error {
            get { return error; }
            set { error = value; }
        }

        private string hint;
        public string Hint {
            get { return hint; }
            set { hint = " (" + value + ")"; }
        }

        bool fatal;
        int line;

        public MException(Interpreter interpreter, string error, string hint = "")
        {
            this.error = error;
            if (hint != "") hint = " (" + hint + ")";
            this.hint = hint;
            fatal = interpreter.Strict;
            line = interpreter.CurrentLine;

            if (fatal) interpreter.Kill();
        }

        public override string TypeName { get { return M_EXCEPTION_TYPENAME; } }

        public override string ToCSString()
        {
            return "*** " + (fatal ? "Fatal error" : "Warning") + "! " + error + " on line " + line.ToString() + hint;
        }
    }
}
