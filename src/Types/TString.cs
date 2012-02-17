using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType representing a string of characters.
    /// </summary>
    class TString : TType
    {
        public string Value { get; set; }

        public TString()
        {
            Value = "";
        }

        public TString(string value)
        {
            Value = value;
        }

        public override string TypeName { get { return T_STRING_TYPENAME; } }

        public override string ToCSString()
        {
            return STRING_CHARACTER + Value + STRING_CHARACTER;
        }
    }
}
