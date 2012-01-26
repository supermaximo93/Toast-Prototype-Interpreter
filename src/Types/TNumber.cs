using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    abstract class TNumber : TType
    {
        public abstract TInteger ToTInteger();
        public abstract TReal ToTReal();
        public abstract TFraction ToTFraction();

        public abstract long TIntegerValue { get; }
        public abstract double TRealValue { get; }

        public abstract TNumber ToNegative();
    }
}
