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
            Simplify();
        }

        private void Simplify()
        {
            if (denominator < 0) // Make denominator positive by making numerator negative instead or cancelling negatives
            {
                numerator = -numerator;
                denominator = -denominator;
            }

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

        public void Add(long otherNumerator, long otherDenominator)
        {
            if (otherDenominator == denominator) numerator += otherNumerator;
            else
            {
                numerator = (numerator * otherDenominator) + (otherNumerator * denominator);
                denominator *= otherDenominator;
            }
            Simplify();
        }

        public void Subtract(long otherNumerator, long otherDenominator)
        {
            if (otherDenominator == denominator) numerator -= otherNumerator;
            else
            {
                numerator = (numerator * otherDenominator) + (otherNumerator * denominator);
                denominator *= otherDenominator;
            }
            Simplify();
        }

        public void Multiply(long otherNumerator, long otherDenominator)
        {
            numerator *= otherNumerator;
            denominator *= otherDenominator;
            Simplify();
        }

        public void Divide(long otherNumerator, long otherDenominator)
        {
            numerator *= otherDenominator;
            denominator *= otherNumerator;
            Simplify();
        }
    }
}
