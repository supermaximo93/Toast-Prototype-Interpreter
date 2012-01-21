using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MFraction : MNumber
    {
        private long numerator;
        public long Numerator
        {
            get { return numerator; }
            set {
                numerator = value;
                Simplify();
            }
        }

        private long denominator;
        public long Denominator
        {
            get { return denominator; }
            set {
                denominator = value;
                MakeDenominatorPositive();
                Simplify();
            }
        }

        public MFraction()
        {
            numerator = 0;
            denominator = 1;
        }

        public MFraction(long numerator, long denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            MakeDenominatorPositive();
            Simplify();
        }

        private void MakeDenominatorPositive()
        {
            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }
        }

        private void Simplify()
        {
            // Use the Euclidean algorithm to find greatest common factor and divide fraction by it
            long a = System.Math.Abs(numerator), b = denominator;
            while ((a > 0) && (b > 0))
            {
                if (a > b) a -= b;
                else b -= a;
            }

            long gcf = a > 0 ? a : b;
            numerator /= gcf;
            denominator /= gcf;
        }

        public override string TypeName { get { return M_FRACTION_TYPENAME; } }

        public override string ToCSString()
        {
            return numerator.ToString() + "/" + denominator.ToString();
        }

        public override MInteger ToMInteger()
        {
            return new MInteger(numerator / denominator);
        }

        public override MReal ToMReal()
        {
            return new MReal((double)numerator / (double)denominator);
        }

        public override MFraction ToMFraction()
        {
            return new MFraction(numerator, denominator);
        }

        public override long MIntegerValue { get { return numerator / denominator; } }
        public override double MRealValue { get { return (double)numerator / (double)denominator; } }

        public override MNumber ToNegative()
        {
            return new MFraction(-numerator, denominator);
        }

        public delegate long Operation(long a, long b);
        public void DoOperation(long otherNumerator, long otherDenominator, Operation operation, bool isLhs)
        {
            if (otherDenominator == denominator)
                numerator = isLhs ? operation(numerator, otherNumerator) : operation(otherNumerator, numerator);
            else if (otherDenominator % denominator == 0)
            {
                long multiplier = otherDenominator / denominator;
                numerator = isLhs ?
                    operation(numerator * multiplier, otherNumerator) : operation(otherNumerator, numerator * multiplier);
                denominator = otherDenominator;
            }
            else if (denominator % otherDenominator == 0)
            {
                long multiplier = denominator / otherDenominator;
                numerator = isLhs ?
                    operation(numerator, otherNumerator * multiplier) : operation(otherNumerator * multiplier, numerator);
            }
            else
            {
                numerator = isLhs ? operation(numerator * otherDenominator, otherNumerator * denominator) :
                    operation(otherNumerator * denominator, numerator * otherDenominator);
                denominator *= otherDenominator;
            }

            MakeDenominatorPositive();
            Simplify();
        }
    }
}
