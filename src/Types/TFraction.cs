using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType/TNumber representing a fraction, stored as two integers (the numerator and denominator)
    /// </summary>
    class TFraction : TNumber
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

        public TFraction()
        {
            numerator = 0;
            denominator = 1;
        }

        public TFraction(long numerator, long denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            Simplify();
        }

        private void Simplify()
        {
            // Make denominator positive by making numerator negative instead or cancelling negatives
            if (denominator < 0)
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

            long gcf = a > 0 ? a : b; // gcf - greatest common factor
            numerator /= gcf;
            denominator /= gcf;
        }

        public override string TypeName { get { return T_FRACTION_TYPENAME; } }

        public override string ToCSString()
        {
            return numerator.ToString() + "/" + denominator.ToString();
        }

        public override TInteger ToTInteger()
        {
            return new TInteger(numerator / denominator);
        }

        public override TReal ToTReal()
        {
            return new TReal((double)numerator / (double)denominator);
        }

        public override TFraction ToTFraction()
        {
            return new TFraction(numerator, denominator);
        }

        public override long TIntegerValue { get { return numerator / denominator; } }
        public override double TRealValue { get { return (double)numerator / (double)denominator; } }

        public override TNumber ToNegative()
        {
            return new TFraction(-numerator, denominator);
        }

        /// <summary>
        /// Adds a fraction to this TFraction.
        /// </summary>
        /// <param name="otherNumerator">The numerator of the fraction to add.</param>
        /// <param name="otherDenominator">The denominator of the fraction to add.</param>
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

        /// <summary>
        /// Subtracts a fraction from this TFraction.
        /// </summary>
        /// <param name="otherNumerator">The numerator of the fraction to subtract by.</param>
        /// <param name="otherDenominator">The denominator of the fraction to subtract by.</param>
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

        /// <summary>
        /// Multiplies this TFraction by a fraction.
        /// </summary>
        /// <param name="otherNumerator">The numerator of the fraction to multiply by.</param>
        /// <param name="otherDenominator">The denominator of the fraction to multiply by.</param>
        public void Multiply(long otherNumerator, long otherDenominator)
        {
            numerator *= otherNumerator;
            denominator *= otherDenominator;
            Simplify();
        }

        /// <summary>
        /// Divides this TFraction by a fraction.
        /// </summary>
        /// <param name="otherNumerator">The numerator of the fraction to divide by.</param>
        /// <param name="otherDenominator">The denominator of the fraction to divide by.</param>
        public void Divide(long otherNumerator, long otherDenominator)
        {
            numerator *= otherDenominator;
            denominator *= otherNumerator;
            Simplify();
        }
    }
}
