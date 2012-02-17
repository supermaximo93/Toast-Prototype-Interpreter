using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType/TNumber representing an integer, stored as a long.
    /// </summary>
    class TInteger : TNumber
    {
        public long Value { get; set; }

        public TInteger()
        {
            Value = 0;
        }

        public TInteger(long initialValue)
        {
            Value = initialValue;
        }

        public override string TypeName { get { return T_INTEGER_TYPENAME; } }

        public override string ToCSString()
        {
            return Value.ToString();
        }

        public override TInteger ToTInteger()
        {
            return new TInteger(Value);
        }

        public override TReal ToTReal()
        {
            return new TReal((double)Value);
        }

        public override TFraction ToTFraction()
        {
            return new TFraction(Value, 1);
        }

        public override long TIntegerValue { get { return Value; } }
        public override double TRealValue { get { return (double)Value; } }

        public override TNumber ToNegative()
        {
            return new TInteger(-Value);
        }
    }
}
