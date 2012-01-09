using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MString : MType
    {
        private string value;
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public MString()
        {
            value = "";
        }

        public MString(string value)
        {
            this.value = value;
        }

        public override string TypeName { get { return M_STRING_TYPENAME; } }

        public override string ToCSString()
        {
            return STRING_CHARACTER + value + STRING_CHARACTER;
        }
    }
}
