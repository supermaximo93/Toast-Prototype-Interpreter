using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types;
using MathsLanguage.Types.Singletons;

namespace MathsLanguage
{
    class Operations
    {
        /// <summary>
        /// Attempts to find an MNumber value for the MType given. If there is no error, but the MNumber reference
        /// given is still null after calling, then the MType is an MString (i.e. some arithmetic will work (+)).
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="number">The MNumber reference to assign the result to.</param>
        /// <param name="value">The MType to get an MNumber out of.</param>
        /// <returns>An exception if there was an error, otherwise null.</returns>
        private static MException AssignNumberValue(Interpreter interpreter, out MNumber number, MType value)
        {
            // Attempt to cast the MType 'value' argument to an MNumber. Failing that, check if it's an MString.
            // If the value is an MNumber or an MString, return null (i.e. no exception). If it's an MVariable then
            // work on the value of the MVariable, otherwise return an exception.
            

            number = value as MNumber;
            if ((number != null) || (value is MString)) return null;

            MVariable variable = value as MVariable;
            if (variable != null)
            {
                value = variable.Value;
                number = value as MNumber;
                if ((number != null) || (value is MString)) return null;
                return new MException(interpreter, "Value of '" + variable.Identifier + "' is not a number",
                    "it is of type '" + value.TypeName + "'");
            }

            return new MException(interpreter, "'" + value.ToCSString() + "' is not a number",
                "it is of type '" + value.TypeName + "'");
        }

        /// <summary>
        /// A static class containing arithmetic methods for MNumbers.
        /// </summary>
        public class Math
        {
            /// <summary>
            /// Takes two MNumbers or two MStrings (or up to two MVariables containing MNumbers or MStrings) and
            /// adds them together.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Add(Interpreter interpreter, MType a, MType b)
            {
                // Convert arguments 'a' and 'b' into either an MNumber or an MString
                MNumber numberA, numberB;
                MString strA = null, strB = null;

                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;

                if (numberA == null)
                {
                    // No errors yet, and numberA is null, so argument 'a' could be an MString or an MVariable
                    // containing an MString
                    strA = a as MString;
                    if (strA == null)
                    {
                        MVariable variable = a as MVariable;
                        if (variable != null) strA = variable.Value as MString;
                    }
                }
                if ((numberA == null) && (strA == null)) // Nothing useful, return an MException
                    return new MException(interpreter, "Value is not a number or string");


                // Same procedure for argument 'b'
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;
                if (numberB == null)
                {
                    strB = b as MString;
                    if (strB == null)
                    {
                        MVariable variable = b as MVariable;
                        if (variable != null) strB = variable.Value as MString;
                    }
                }
                if ((numberB == null) && (strB == null))
                    return new MException(interpreter, "Value is not a number or string");

                // Attempt addition if both operands are the same type, otherwise return an MException
                if ((numberB == null) && (strA == null))
                    return new MException(interpreter, "Attempted addition of a string to a number");
                else if ((numberA == null) && (strB == null))
                    return new MException(interpreter, "Attempted addition of a number to a string");
                else if ((numberA == null) && (numberB == null))
                {
                    return new MString(strA.Value + strB.Value);
                }
                else
                {
                    //The left hand operand decides the type of the returned value
                    switch (numberA.TypeName)
                    {
                        case MType.M_INTEGER_TYPENAME:
                            {
                                // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                                MFraction fraction = numberB as MFraction;
                                if (fraction != null)
                                {
                                    // Copy the right hand fraction and add the left hand integer to it
                                    fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                                    fraction.Add(numberA.MIntegerValue, 1);
                                    return fraction;
                                }
                                return new MInteger(numberA.MIntegerValue + numberB.MIntegerValue);
                            }

                        case MType.M_REAL_TYPENAME:
                            return new MReal(numberA.MRealValue + numberB.MRealValue);

                        case MType.M_FRACTION_TYPENAME:
                            {
                                // Create a copy of the left hand fraction
                                MFraction fraction = numberA as MFraction;
                                fraction = new MFraction(fraction.Numerator, fraction.Denominator);

                                // Convert the right hand operand to a fraction
                                long numerator, denominator;
                                MFraction otherFraction = numberB as MFraction;
                                if (otherFraction != null) // If it's a fraction, simply copy the values
                                {
                                    numerator = otherFraction.Numerator;
                                    denominator = otherFraction.Denominator;
                                }
                                else
                                {
                                    // Check if it's an MInteger first. It might not need to use DoubleToFraction
                                    if (numberB is MInteger)
                                    {
                                        numerator = numberB.MIntegerValue;
                                        denominator = 1;
                                    }
                                    else Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator,
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
            /// Takes two MNumbers and subtracts one from the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Subtract(Interpreter interpreter, MType a, MType b)
            {
                // Try to get MNumber values from the MType arguments
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be an MString; check them
                if ((numberA == null) || (numberB == null))
                    return new MException(interpreter, "Strings cannot be used in subtraction operations");

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            MFraction rhsFraction = numberB as MFraction;
                            if (rhsFraction != null)
                            {
                                // Order of fractions matters in this case
                                MFraction lhsFraction = new MFraction(numberA.MIntegerValue, 1);
                                lhsFraction.Subtract(rhsFraction.Numerator, rhsFraction.Denominator);
                                return lhsFraction;
                            }
                            return new MInteger(numberA.MIntegerValue - numberB.MIntegerValue);
                        }

                    case MType.M_REAL_TYPENAME:
                        return new MReal(numberA.MRealValue - numberB.MRealValue);

                    case MType.M_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's an MInteger first. It might not need to use DoubleToFraction
                                if (numberB is MInteger)
                                {
                                    numerator = numberB.MIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Subtract(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two MNumbers and multiplies them together.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Multiply(Interpreter interpreter, MType a, MType b)
            {
                // Try to get MNumber values from the MType arguments
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be an MString; check them
                if ((numberA == null) || (numberB == null))
                    return new MException(interpreter, "Strings cannot be used in multiplication operations");

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            MFraction fraction = numberB as MFraction;
                            if (fraction != null)
                            {
                                // Copy the right hand fraction and multiply it by the left hand integer
                                fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                                fraction.Multiply(numberA.MIntegerValue, 1);
                                return fraction;
                            }
                            return new MInteger(numberA.MIntegerValue * numberB.MIntegerValue);
                        }

                    case MType.M_REAL_TYPENAME:
                        return new MReal(numberA.MRealValue * numberB.MRealValue);

                    case MType.M_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's an MInteger first. It might not need to use DoubleToFraction
                                if (numberB is MInteger)
                                {
                                    numerator = numberB.MIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Multiply(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two MNumbers and divides one by the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Divide(Interpreter interpreter, MType a, MType b)
            {
                // Try to get MNumber values from the MType arguments
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be an MString; check them
                if ((numberA == null) || (numberB == null))
                    return new MException(interpreter, "Strings cannot be used in division operations");

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            // If the other operand is a fraction, treat this integer as a fraction (i.e. value/1)
                            MFraction rhsFraction = numberB as MFraction;
                            if (rhsFraction != null)
                            {
                                // Order of fractions matters in this case
                                MFraction lhsFraction = new MFraction(numberA.MIntegerValue, 1);
                                lhsFraction.Divide(rhsFraction.Numerator, rhsFraction.Denominator);
                                return lhsFraction;
                            }
                            if (numberB.TypeName == MType.M_INTEGER_TYPENAME)
                                return new MFraction(numberA.MIntegerValue, numberB.MIntegerValue);
                            return new MInteger(numberA.MIntegerValue / numberB.MIntegerValue);
                        }

                    case MType.M_REAL_TYPENAME:
                        return new MReal(numberA.MRealValue / numberB.MRealValue);

                    case MType.M_FRACTION_TYPENAME:
                        {
                            // Create a copy of the left hand fraction
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);

                            // Convert the right hand operand to a fraction
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction != null) // If it's a fraction, simply copy the values
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            else
                            {
                                // Check if it's an MInteger first. It might not need to use DoubleToFraction
                                if (numberB is MInteger)
                                {
                                    numerator = numberB.MIntegerValue;
                                    denominator = 1;
                                }
                                else Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator,
                                    out denominator);
                            }

                            fraction.Divide(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            /// <summary>
            /// Takes two MNumbers and returns the first one to the power of the other.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the operation.</param>
            /// <param name="b">The right hand operand of the operation.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Pow(Interpreter interpreter, MType a, MType b)
            {
                // Try to get MNumber values from the MType arguments
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be an MString; check them
                if ((numberA == null) || (numberB == null))
                    return new MException(interpreter, "Strings cannot be used in exponentiation operations");

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        return new MInteger(
                            (long)System.Math.Round(System.Math.Pow(numberA.MRealValue, numberB.MRealValue)));

                    case MType.M_REAL_TYPENAME:
                        return new MReal(System.Math.Pow(numberA.MRealValue, numberB.MRealValue));

                    case MType.M_FRACTION_TYPENAME:
                        MFraction fraction = numberA as MFraction;
                        long numerator =
                            (long)System.Math.Round(System.Math.Pow(fraction.Numerator, numberB.MRealValue));
                        long denominator =
                            (long)System.Math.Round(System.Math.Pow(fraction.Denominator, numberB.MRealValue));
                        return new MFraction(numerator, denominator);
                }

                return null;
            }

            /// <summary>
            /// Takes an MNumber and returns its absolute value.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="value">The MNumber to get the absolute value of.</param>
            /// <returns>
            /// The MType resulting from the operation. An MExcpetion or null is returned when there is an error.
            /// </returns>
            public static MType Modulus(Interpreter interpreter, MType value)
            {
                MNumber number;
                MException exception = AssignNumberValue(interpreter, out number, value);
                if (exception != null) return exception;

                // No errors yet, but make sure it's not a string
                if (number == null) return new MException(interpreter, "Strings cannot be used in modulus operations");

                switch (number.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        return new MInteger(System.Math.Abs(number.MIntegerValue));
                    case MType.M_REAL_TYPENAME:
                        return new MReal(System.Math.Abs(number.MRealValue));
                    case MType.M_FRACTION_TYPENAME:
                        MFraction fraction = number as MFraction;
                        return new MFraction(System.Math.Abs(fraction.Numerator), fraction.Denominator);
                        // No need to abs denominator as MFraction denominators are automatically kept positive
                }

                return null;
            }

            /// <summary>
            /// Compares one MNumber with another based on a given inequality operator.
            /// </summary>
            /// <param name="interpreter">The interpreter that the method is being called from.</param>
            /// <param name="a">The left hand operand of the comparison.</param>
            /// <param name="b">The right hand operand of the comparison.</param>
            /// <param name="inequality">The inequality operator to use in the comparison.</param>
            /// <returns>
            /// An MBoolean containing the result of the comparison. Returns an MException when there is an error.
            /// </returns>
            public static MType Inequality(Interpreter interpreter, MType a, MType b, string inequality)
            {
                // Try to get MNumber values from the MType arguments
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                // No errors, but one or both of the arguments could be an MString; check them
                if ((numberA == null) || (numberB == null))
                    return new MException(interpreter, "Strings cannot be used in inequality comparisons");

                bool result;
                switch (inequality)
                {
                    case ">":
                        result = numberA.MRealValue > numberB.MRealValue;
                        break;
                    case ">=":
                        result = numberA.MRealValue >= numberB.MRealValue;
                        break;
                    case "<":
                        result = numberA.MRealValue < numberB.MRealValue;
                        break;
                    case "<=":
                        result = numberA.MRealValue <= numberB.MRealValue;
                        break;
                    default:
                        return new MException(interpreter, "Invalid inequality operator given");
                }

                return new MBoolean(result);
            }
        }

        /// <summary>
        /// Does an equality comparison of one MType with another.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="a">The left hand operand of the comparison.</param>
        /// <param name="b">The right hand operand of the comparison.</param>
        /// <param name="strict">
        /// Whether the equality should be approximate or not. Give true for exact equality comparisons, and false for
        /// approximate equality comparisons of MNumbers.
        /// </param>
        /// <returns>
        /// An MBoolean containing the result of the comparison. Returns an MException or null when there is an error.
        /// </returns>
        public static MType Equal(Interpreter interpreter, MType a, MType b, bool strict)
        {
            // If arguments are MVariable, get their value. The values of the MVariable need to be compared,
            // not the MVariable objects themselves
            MVariable variable = a as MVariable;
            if (variable != null) a = variable.Value;
            variable = b as MVariable;
            if (variable != null) b = variable.Value;

            // Make sure that each operand is of the same type. MNumbers are an exception; any MNumber derivative can
            // be compared with any other MNumber derivative
            if ((a.TypeName != b.TypeName) && !((a is MNumber) && (b is MNumber)))
                return new MException(interpreter,
                    "Type '" + a.TypeName + "' cannot be compared with type '" + b.TypeName + "'");

            // Using 'as' syntax instead of '()' for casting, because it looks cleaner. The result of the 'as' will not
            // return null because we've done the necessary check beforehand (i.e. if 'a' is an MNumber, then 'b' must
            // also be an MNumber)
            if (a is MNumber)
            {
                bool result;
                if (strict) result = ((a as MNumber).MRealValue == (b as MNumber).MRealValue);
                else result = ((a as MNumber).MIntegerValue == (b as MNumber).MIntegerValue);

                return new MBoolean(result);
            }
            else if (a is MBoolean)
            {
                return new MBoolean((a as MBoolean).Value == (b as MBoolean).Value);
            }
            else if (a is MString) {
                return new MBoolean((a as MString).Value == (b as MString).Value);
            }
            else if (a is MVariable) // i.e. if argument 'a' is a reference
            {
                return new MBoolean(a == b);
            }
            else if (a is MFunction)
            {
                MFunction funcA = a as MFunction, funcB = b as MFunction;
                return new MBoolean(
                    (funcA.HardCodedFunction == funcB.HardCodedFunction) &&
                    (funcA.CustomFunction == funcB.CustomFunction) &&
                    (funcA.Block == funcB.Block));
            }
            else if (a is MNil)
            {
                return new MBoolean(b is MNil);
            }

            return null;
        }

        /// <summary>
        /// Does a 'not equal to' inequality comparison of one MType with another. Always does a strict comparison,
        /// unlike the Operations.Equal method where strictness can be specified.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="a">The left hand operand of the comparison.</param>
        /// <param name="b">The right hand operand of the comparison.</param>
        /// <returns>
        /// An MBoolean containing the result of the comparison. Returns an MException or null when there is an error.
        /// </returns>
        public static MType NotEqual(Interpreter interpreter, MType a, MType b)
        {
            // Do an equality comparison of the arguments, and return the result if it's an MException or null.
            // If the result is an MBoolean then invert its value
            MType value = Equal(interpreter, a, b, true);
            MBoolean result = value as MBoolean;
            if (result == null) return value;
            result.Value = !result.Value;
            return result;
        }

        /// <summary>
        /// Methods that are for general use in the program and not necessarily for use with MTypes.
        /// </summary>
        public class Misc
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
