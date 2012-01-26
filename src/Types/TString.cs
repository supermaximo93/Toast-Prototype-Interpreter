using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TString : TType
    {
        private string value;
        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public TString()
        {
            value = "";
        }

        public TString(string value)
        {
            this.value = value;
        }

        public override string TypeName { get { return T_STRING_TYPENAME; } }

        public override string ToCSString()
        {
            return STRING_CHARACTER + value + STRING_CHARACTER;
        }
    }
}
