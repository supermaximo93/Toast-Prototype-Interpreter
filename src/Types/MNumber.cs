using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    abstract class MNumber : MType
    {
        public abstract MInteger ToMInteger();
        public abstract MReal ToMReal();

        public abstract long MIntegerValue { get; }
        public abstract double MRealValue { get; }

        public abstract MNumber ToNegative();
    }
}
