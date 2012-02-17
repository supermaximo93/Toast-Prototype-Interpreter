using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType that contains another TType. It can even contain another TVariable in order to act as a reference to
    /// that TVariable. TVariables by default contain a zeroed TInteger.
    /// </summary>
    class TVariable : TType
    {
        public string Identifier { get; private set; }

        public TType Value { get; set; }

        public TVariable(string name)
        {
            Identifier = name;
            Value = new TInteger(0);
        }

        public TVariable(string name, TType value)
        {
            Identifier = name;
            Value = value;
        }

        public override string TypeName { get { return T_VARIABLE_TYPENAME; } }

        public override string ToCSString()
        {
            return Identifier + (Value is TVariable ? " => " : " = ") + Value.ToCSString();
        }
    }
}
