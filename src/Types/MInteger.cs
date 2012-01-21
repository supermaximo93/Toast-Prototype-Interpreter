using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MInteger : MNumber
    {
        private long value;
        public long Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public MInteger()
        {
            value = 0;
        }

        public MInteger(long initialValue)
        {
            value = initialValue;
        }

        public override string TypeName { get { return M_INTEGER_TYPENAME; } }

        public override string ToCSString()
        {
            return value.ToString();
        }

        public override MInteger ToMInteger()
        {
            return new MInteger(value);
        }

        public override MReal ToMReal()
        {
            return new MReal((double)value);
        }

        public override MFraction ToMFraction()
        {
            long numerator, denominator;
            Operations.Misc.DoubleToFraction((double)value, out numerator, out denominator);
            return new MFraction(numerator, denominator);
        }

        public override long MIntegerValue { get { return value; } }
        public override double MRealValue { get { return (double)value; } }

        public override MNumber ToNegative()
        {
            return new MInteger(-value);
        }
    }
}
