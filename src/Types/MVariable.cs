using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MVariable : MType
    {
        string identifier;
        public string Identifier { get { return identifier; } }

        MType value;
        public MType Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public MVariable(string name)
        {
            identifier = name;
            value = new MInteger(0);
        }

        public MVariable(string name, MType value)
        {
            identifier = name;
            this.value = value;
        }

        public override string TypeName { get { return M_VARIABLE_TYPENAME; } }

        public override string ToCSString()
        {
            return identifier + (value is MVariable ? " => " : " = ") + value.ToCSString();
        }
    }
}
