using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TVariable : TType
    {
        string identifier;
        public string Identifier { get { return identifier; } }

        TType value;
        public TType Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public TVariable(string name)
        {
            identifier = name;
            value = new TInteger(0);
        }

        public TVariable(string name, TType value)
        {
            identifier = name;
            this.value = value;
        }

        public override string TypeName { get { return T_VARIABLE_TYPENAME; } }

        public override string ToCSString()
        {
            return identifier + (value is TVariable ? " => " : " = ") + value.ToCSString();
        }
    }
}
