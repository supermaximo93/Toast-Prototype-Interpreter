using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TInteger : TNumber
    {
        private long value;
        public long Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public TInteger()
        {
            value = 0;
        }

        public TInteger(long initialValue)
        {
            value = initialValue;
        }

        public override string TypeName { get { return T_INTEGER_TYPENAME; } }

        public override string ToCSString()
        {
            return value.ToString();
        }

        public override TInteger ToTInteger()
        {
            return new TInteger(value);
        }

        public override TReal ToTReal()
        {
            return new TReal((double)value);
        }

        public override TFraction ToTFraction()
        {
            return new TFraction(value, 1);
        }

        public override long TIntegerValue { get { return value; } }
        public override double TRealValue { get { return (double)value; } }

        public override TNumber ToNegative()
        {
            return new TInteger(-value);
        }
    }
}
