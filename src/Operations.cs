using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types;
using Toast.Types.Singletons;

namespace Toast
{
    /// <summary>
    /// A static class containing methods for evaluating expressions.
    /// </summary>
    static class Operations
    {
        /// <summary>
        /// Attempts to find a TNumber value for the TType given. If there is no error, but the TNumber reference
        /// given is still null after calling, then the TType is a TString (i.e. some arithmetic will work (+)).
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="number">The TNumber reference to assign the result to.</param>
        /// <param name="value">The TType to get a TNumber out of.</param>
        /// <returns>An exception if there was an error, otherwise null.</returns>
        private static TException AssignNumberValue(Interpreter interpreter, out TNumber number, TType value)
        {
            // Attempt to cast the TType 'value' argument to a TNumber. Failing that, check if it's a TString.
            // If the value is a TNumber or a TString, return null (i.e. no exception). If it's a TVariable then
            // work on the value of the TVariable, otherwise return an exception.
            

            number = value as TNumber;
            if ((number != null) || (value is TString)) return null;

            TVariable variable = value as TVariable;
            if (variable != null)
            {
                value = variable.Value;
                number = value as TNumber;
                if ((number != null) || (value is TString)) return null;
                return new TException(interpreter, "Value of '" + variable.Identifier + "' is not a number",
                    "it is of type '" + value.TypeName + "'");
            }

            return new TException(interpreter, "'" + value.ToCSString() + "' is not a number",
                "it is of type '" + value.TypeName + "'");
        }

        /// <summary>
        /// A static class containing arithmetic methods for TNumbers.
        /// </summary>
        public static class Math
        {
            /// <summary>
            /// Takes two TNumbers or two TStrings (or up to two TVariables containing TNumbers or TStrings) and
            /// adds them together.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Add(Interpreter interpreter, TType a, TType b)
            {
                // Convert arguments 'a' and 'b' into either a TNumber or a TString
                TNumber numberA, numberB;
                TString strA = null, strB = null;

                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;

                if (numberA == null)
                {
                    // No errors yet, and numberA is null, so argument 'a' could be a TString or a TVariable
                    // containing a TString
                    strA = a as TString;
                    if (strA == null)
                    {
                        TVariable variable = a as TVariable;
                        if (variable != null) strA = variable.Value as TString;
                    }
                }
                if ((numberA == null) && (strA == null)) // Nothing useful, return a TException
                    return new TException(interpreter, "Value is not a number or string");


                // Same procedure for argument 'b'
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;
                if (numberB == null)
                {
                    strB = b as TString;
                    if (strB == null)
                    {
                        TVariable variable = b as TVariable;
                        if (variable != null) strB = variable.Value as TString;
                    }
                }
                if ((numberB == null) && (strB == null))
                    return new TException(interpreter, "Value is not a number or string");

                // Attempt addition if both operands are the same type, otherwise return a TException
                if ((numberB == null) && (strA == null))
                    return new TException(interpreter, "Attempted addition of a string to a number");
                else if ((numberA == null) && (strB == null))
                    return new TException(interpreter, "Attempted addition of a number to a string");
                else if ((numberA == null) && (numberB == null))
                {
                    return new TString(strA.Value + strB.Value);
                }
                else
                {
                    //The left hand operand decides the type of the returned value
                    switch (numberA.TypeName)
                    {
                        case TType.T_INTEGER_TYPENAME:
                            {
                                // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                                TFraction fraction = numberB as TFraction;
                                if (fraction != null)
                                {
                                    // Copy the right hand fraction and add the left hand integer to it
                                    fraction = new TFraction(fraction.Numerator, fraction.Denominator);
                                    fraction.Add(numberA.TIntegerValue, 1);
                                    return fraction;
                                }
                                return new TInteger(numberA.TIntegerValue + numberB.TIntegerValue);
                            }

                        case TType.T_REAL_TYPENAME:
                            return new TReal(numberA.TRealValue + numberB.TRealValue);

                        case TType.T_FRACTION_TYPENAME:
                            {
                                // Create a copy of the left hand fraction
                                TFraction fraction = numberA as TFraction;
                                fraction = new TFraction(fraction.Numerator, fraction.Denominator);

                                // Convert the right hand operand to a fraction
                                long numerator, denominator;
                                TFraction otherFraction = numberB as TFraction;
                                if (otherFraction != null) // If it's a fraction, simply copy the values
                                {
                                    numerator = otherFraction.Numerator;
                                    denominator = otherFraction.Denominator;
                                }
                                else
                                {
                                    // Check if it's a TInteger first. It might not need to use DoubleToFraction
                                    if (numberB is TInteger)
                                    {
                                        numerator = numberB.TIntegerValue;
                                        denominator = 1;
                                    }
                                    else Operations.Misc.DoubleToFraction(numberB.TRealValue, out numerator,
                                        out denominator);
                                }

                                fraction.Add(numerator, denominator);
                                return fraction;
                            }
                    }
                }

                return null;
            }

            /// <summary>
            /// Takes two TNumbers and subtracts one from the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Subtract(Interpreter interpreter, TType a, TType b)
            {
                // Try to get TNumber values from the TType arguments
                TNumber numberA, numberB;
                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be a TString; check them
                if ((numberA == null) || (numberB == null))
                    return new TException(interpreter, "Strings cannot be used in subtraction operations");

                switch (numberA.TypeName)
                {
                    case TType.T_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            TFraction rhsFraction = numberB as TFraction;
                            if (rhsFraction != null)
                            {
                                // Order of fractions matters in this case
                                TFraction lhsFraction = new TFraction(numberA.TIntegerValue, 1);
                                lhsFraction.Subtract(rhsFraction.Numerator, rhsFraction.Denominator);
                                return lhsFraction;
                            }
                            return new TInteger(numberA.TIntegerValue - numberB.TIntegerValue);
                        }

                    case TType.T_REAL_TYPENAME:
                        return new TReal(numberA.TRealValue - numberB.TRealValue);

                    case TType.T_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            TFraction fraction = numberA as TFraction;
                            fraction = new TFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            TFraction otherFraction = numberB as TFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's a TInteger first. It might not need to use DoubleToFraction
                                if (numberB is TInteger)
                                {
                                    numerator = numberB.TIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.TRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Subtract(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two TNumbers and multiplies them together.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Multiply(Interpreter interpreter, TType a, TType b)
            {
                // Try to get TNumber values from the TType arguments
                TNumber numberA, numberB;
                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be a TString; check them
                if ((numberA == null) || (numberB == null))
                    return new TException(interpreter, "Strings cannot be used in multiplication operations");

                switch (numberA.TypeName)
                {
                    case TType.T_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            TFraction fraction = numberB as TFraction;
                            if (fraction != null)
                            {
                                // Copy the right hand fraction and multiply it by the left hand integer
                                fraction = new TFraction(fraction.Numerator, fraction.Denominator);
                                fraction.Multiply(numberA.TIntegerValue, 1);
                                return fraction;
                            }
                            return new TInteger(numberA.TIntegerValue * numberB.TIntegerValue);
                        }

                    case TType.T_REAL_TYPENAME:
                        return new TReal(numberA.TRealValue * numberB.TRealValue);

                    case TType.T_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            TFraction fraction = numberA as TFraction;
                            fraction = new TFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            TFraction otherFraction = numberB as TFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's a TInteger first. It might not need to use DoubleToFraction
                                if (numberB is TInteger)
                                {
                                    numerator = numberB.TIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.TRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Multiply(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two TNumbers and divides one by the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Divide(Interpreter interpreter, TType a, TType b)
            {
                // Try to get TNumber values from the TType arguments
                TNumber numberA, numberB;
                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be a TString; check them
                if ((numberA == null) || (numberB == null))
                    return new TException(interpreter, "Strings cannot be used in division operations");

                switch (numberA.TypeName)
                {
                    case TType.T_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            TFraction rhsFraction = numberB as TFraction;
                            if (rhsFraction != null)
                            {
                                // Order of fractions matters in this case
                                TFraction lhsFraction = new TFraction(numberA.TIntegerValue, 1);
                                lhsFraction.Divide(rhsFraction.Numerator, rhsFraction.Denominator);
                                return lhsFraction;
                            }
                            if (numberB.TypeName == TType.T_INTEGER_TYPENAME)
                                return new TFraction(numberA.TIntegerValue, numberB.TIntegerValue);
                            return new TInteger(numberA.TIntegerValue / numberB.TIntegerValue);
                        }

                    case TType.T_REAL_TYPENAME:
                        return new TReal(numberA.TRealValue / numberB.TRealValue);

                    case TType.T_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            TFraction fraction = numberA as TFraction;
                            fraction = new TFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            TFraction otherFraction = numberB as TFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's a TInteger first. It might not need to use DoubleToFraction
                                if (numberB is TInteger)
                                {
                                    numerator = numberB.TIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.TRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Divide(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two TNumbers and returns the first one to the power of the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Pow(Interpreter interpreter, TType a, TType b)
            {
                // Try to get TNumber values from the TType arguments
                TNumber numberA, numberB;
                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be a TString; check them
                if ((numberA == null) || (numberB == null))
                    return new TException(interpreter, "Strings cannot be used in exponentiation operations");

                switch (numberA.TypeName)
                {
                    case TType.T_INTEGER_TYPENAME:
                        return new TInteger(
                            (long)System.Math.Round(System.Math.Pow(numberA.TRealValue, numberB.TRealValue)));

                    case TType.T_REAL_TYPENAME:
                        return new TReal(System.Math.Pow(numberA.TRealValue, numberB.TRealValue));

                    case TType.T_FRACTION_TYPENAME:
                        TFraction fraction = numberA as TFraction;
                        long numerator =
                            (long)System.Math.Round(System.Math.Pow(fraction.Numerator, numberB.TRealValue));
                        long denominator =
                            (long)System.Math.Round(System.Math.Pow(fraction.Denominator, numberB.TRealValue));
                        return new TFraction(numerator, denominator);
                }

                return null;
            }

            /// <summary>
            /// Takes a TNumber and returns its absolute value.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="value">The TNumber to get the absolute value of.</param>
            /// <returns>
            /// The TType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static TType Modulus(Interpreter interpreter, TType value)
            {
                TNumber number;
                TException exception = AssignNumberValue(interpreter, out number, value);
                if (exception != null) return exception;

                // No errors yet, but make sure it's not a string
                if (number == null) return new TException(interpreter, "Strings cannot be used in modulus operations");

                switch (number.TypeName)
                {
                    case TType.T_INTEGER_TYPENAME:
                        return new TInteger(System.Math.Abs(number.TIntegerValue));
                    case TType.T_REAL_TYPENAME:
                        return new TReal(System.Math.Abs(number.TRealValue));
                    case TType.T_FRACTION_TYPENAME:
                        TFraction fraction = number as TFraction;
                        return new TFraction(System.Math.Abs(fraction.Numerator), fraction.Denominator);
                        // No need to abs denominator as TFraction denominators are automatically kept positive
                }

                return null;
            }

            /// <summary>
            /// Compares one TNumber with another based on a given inequality operator.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the comparison.</param>
            /// <param name="b">The right hand operand of the comparison.</param>
            /// <param name="inequality">The inequality operator to use in the comparison.</param>
            /// <returns>
            /// An TBoolean containing the result of the comparison. Returns a TException when there is an error.
            /// </returns>
            public static TType Inequality(Interpreter interpreter, TType a, TType b, string inequality)
            {
                // Try to get TNumber values from the TType arguments
                TNumber numberA, numberB;
                TException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be a TString; check them
                if ((numberA == null) || (numberB == null))
                    return new TException(interpreter, "Strings cannot be used in inequality comparisons");

                bool result;
                switch (inequality)
                {
                    case ">":
                        result = numberA.TRealValue > numberB.TRealValue;
                        break;
                    case ">=":
                        result = numberA.TRealValue >= numberB.TRealValue;
                        break;
                    case "<":
                        result = numberA.TRealValue < numberB.TRealValue;
                        break;
                    case "<=":
                        result = numberA.TRealValue <= numberB.TRealValue;
                        break;
                    default:
                        return new TException(interpreter, "Invalid inequality operator given");
                }

                return new TBoolean(result);
            }
        }

        /// <summary>
        /// Does an equality comparison of one TType with another.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="a">The left hand operand of the comparison.</param>
        /// <param name="b">The right hand operand of the comparison.</param>
        /// <param name="strict">
        /// Whether the equality should be approximate or not. Give true for exact equality comparisons, and false for
        /// approximate equality comparisons of TNumbers.
        /// </param>
        /// <returns>
        /// An TBoolean containing the result of the comparison. Returns a TException or null when there is an error.
        /// </returns>
        public static TType Equal(Interpreter interpreter, TType a, TType b, bool strict)
        {
            // If arguments are TVariable, get their value. The values of the TVariable need to be compared,
            // not the TVariable objects themselves
            TVariable variable = a as TVariable;
            if (variable != null) a = variable.Value;
            variable = b as TVariable;
            if (variable != null) b = variable.Value;

            // Make sure that each operand is of the same type. TNumbers are an exception; any TNumber derivative can
            // be compared with any other TNumber derivative
            if ((a.TypeName != b.TypeName) && !((a is TNumber) && (b is TNumber)))
                return new TException(interpreter,
                    "Type '" + a.TypeName + "' cannot be compared with type '" + b.TypeName + "'");

            // Using 'as' syntax instead of '()' for casting, because it looks cleaner. The result of the 'as' will not
            // return null because we've done the necessary check beforehand (i.e. if 'a' is a TNumber, then 'b' must
            // also be a TNumber)
            if (a is TNumber)
            {
                bool result;
                if (strict) result =
                    (System.Math.Abs((a as TNumber).TRealValue - (b as TNumber).TRealValue) < 0.000001);
                else
                {
                    double aVal = (a as TNumber).TRealValue, bVal = (b as TNumber).TRealValue;
                    if ((System.Math.Round(aVal) == bVal) || (System.Math.Round(bVal) == aVal)) result = true;
                    else result = (System.Math.Abs(aVal - bVal) < 0.5);
                }
                return new TBoolean(result);
            }
            else if (a is TBoolean)
            {
                return new TBoolean((a as TBoolean).Value == (b as TBoolean).Value);
            }
            else if (a is TString) {
                return new TBoolean((a as TString).Value == (b as TString).Value);
            }
            else if (a is TVariable) // i.e. if argument 'a' is a reference
            {
                return new TBoolean(a == b);
            }
            else if (a is TFunction)
            {
                TFunction funcA = a as TFunction, funcB = b as TFunction;
                return new TBoolean(
                    (funcA.HardCodedFunction == funcB.HardCodedFunction) &&
                    (funcA.CustomFunction == funcB.CustomFunction) &&
                    (funcA.Block == funcB.Block));
            }
            else if (a is TNil)
            {
                return new TBoolean(b is TNil);
            }

            return null;
        }

        /// <summary>
        /// Does a 'not equal to' inequality comparison of one TType with another. Always does a strict comparison,
        /// unlike the Operations.Equal method where strictness can be specified.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="a">The left hand operand of the comparison.</param>
        /// <param name="b">The right hand operand of the comparison.</param>
        /// <returns>
        /// An TBoolean containing the result of the comparison. Returns a TException or null when there is an error.
        /// </returns>
        public static TType NotEqual(Interpreter interpreter, TType a, TType b)
        {
            // Do an equality comparison of the arguments, and return the result if it's a TException or null.
            // If the result is a TBoolean then invert its value
            TType value = Equal(interpreter, a, b, true);
            TBoolean result = value as TBoolean;
            if (result == null) return value;
            result.Value = !result.Value;
            return result;
        }

        /// <summary>
        /// A static class containing methods that are for general use in the program and not necessarily for use with
        /// TTypes.
        /// </summary>
        public static class Misc
        {
            /// <summary>
            /// Converts a floating point value into a fraction.
            /// Algorithm taken from http://homepage.smc.edu/kennedy_john/DEC2FRAC.PDF by John Kennedy.
            /// </summary>
            /// <param name="value">The value to convert into a fraction.</param>
            /// <param name="numerator">The numerator of the resulting fraction.</param>
            /// <param name="denominator">The denominator of the resulting fraction.</param>
            public static void DoubleToFraction(double value, out long numerator, out long denominator)
            {
                const double precision = 0.000005;

                long sign, previousDenominator, scratchValue;
                double z;

                if (value < 0.0) sign = -1;
                else sign = 1;
                value = System.Math.Abs(value);

                if (value == (long)value)
                {
                    numerator = (long)(value * sign);
                    denominator = 1;
                    return;
                }

                if (value < 1.0E-18)
                {
                    numerator = sign;
                    denominator = 999999999999999999;
                    return;
                }

                if (value > 1.0E+18)
                {
                    numerator = 999999999999999999 * sign;
                    denominator = 1;
                    return;
                }

                z = value;
                previousDenominator = 0;
                denominator = 1;

                do
                {
                    z = 1.0 / (z - (long)z);
                    scratchValue = denominator;
                    denominator = (denominator * (long)z) + previousDenominator;
                    previousDenominator = scratchValue;
                    numerator = (long)((value * (double)denominator) + 0.5);
                } while (!((System.Math.Abs(value - ((double)numerator / (double)denominator)) < precision) ||
                    (z == (long)z)));
            }
        }
    }
}
