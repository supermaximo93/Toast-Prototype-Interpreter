using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MReal : MNumber
    {
        private double value;
        public double Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public MReal()
        {
            value = 0.0;
        }

        public MReal(double initialValue)
        {
            value = initialValue;
        }

        public override string TypeName { get { return M_REAL_TYPENAME; } }

        public override string ToCSString()
        {
            return value.ToString();
        }

        public override MInteger ToMInteger()
        {
            return new MInteger((Int64)Math.Round(value));
        }

        public override MReal ToMReal()
        {
            return this;
        }

        public override long MIntegerValue { get { return (long)Math.Round(value); } }
        public override double MRealValue { get { return value; } }

        public override MNumber ToNegative()
        {
            return new MReal(-value);
        }
    }
}
