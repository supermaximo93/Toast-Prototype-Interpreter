using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType/TNumber representing a real number, which is stored as a double.
    /// </summary>
    class TReal : TNumber
    {
        public double Value { get; set; }

        public TReal()
        {
            Value = 0.0;
        }

        public TReal(double initialValue)
        {
            Value = initialValue;
        }

        public override string TypeName { get { return T_REAL_TYPENAME; } }

        public override string ToCSString()
        {
            return Value.ToString();
        }

        public override TInteger ToTInteger()
        {
            return new TInteger((Int64)Math.Round(Value));
        }

        public override TReal ToTReal()
        {
            return new TReal(Value);
        }

        public override TFraction ToTFraction()
        {
            long numerator, denominator;
            Operations.Misc.DoubleToFraction(Value, out numerator, out denominator);
            return new TFraction(numerator, denominator);
        }

        public override long TIntegerValue { get { return (long)Math.Round(Value); } }
        public override double TRealValue { get { return Value; } }

        public override TNumber ToNegative()
        {
            return new TReal(-Value);
        }
    }
}
