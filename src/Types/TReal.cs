using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TReal : TNumber
    {
        private double value;
        public double Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public TReal()
        {
            value = 0.0;
        }

        public TReal(double initialValue)
        {
            value = initialValue;
        }

        public override string TypeName { get { return T_REAL_TYPENAME; } }

        public override string ToCSString()
        {
            return value.ToString();
        }

        public override TInteger ToTInteger()
        {
            return new TInteger((Int64)Math.Round(value));
        }

        public override TReal ToTReal()
        {
            return new TReal(value);
        }

        public override TFraction ToTFraction()
        {
            long numerator, denominator;
            Operations.Misc.DoubleToFraction(value, out numerator, out denominator);
            return new TFraction(numerator, denominator);
        }

        public override long TIntegerValue { get { return (long)Math.Round(value); } }
        public override double TRealValue { get { return value; } }

        public override TNumber ToNegative()
        {
            return new TReal(-value);
        }
    }
}
