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
        private static MException AssignNumberValue(Interpreter interpreter, out MNumber number, MType value)
        {
            number = null;

            if (value is MNumber) number = (MNumber)value;
            else if (value is MVariable)
            {
                MVariable variable = (MVariable)value;
                if (variable.Value is MNumber) number = (MNumber)variable.Value;
                else if (variable.Value is MString) number = null;
                else return new MException(interpreter, "Value of '" + variable.Identifier + "' is not a number");
            }
            else if (value is MString) number = null;
            else return new MException(interpreter, "'" + value.ToCSString() + "' is not a number");

            return null;
        }

        public class Math
        {
            public static MType Add(Interpreter interpreter, MType a, MType b)
            {
                MNumber numberA, numberB;
                MString strA = null, strB = null;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                if (numberA == null)
                {
                    strA = a as MString;
                    if (strA == null)
                    {
                        MVariable variable = a as MVariable;
                        if (variable != null) strA = variable.Value as MString;
                    }
                }
                if ((numberA == null) && (strA == null)) return new MException(interpreter, "Value is not a number or string");

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
                if ((numberB == null) && (strB == null)) return new MException(interpreter, "Value is not a number or string");

                if ((numberB == null) && (strA == null)) return new MException(interpreter, "Attempted addition of a string to a number");
                else if ((numberA == null) && (strB == null)) return new MException(interpreter, "Attempted addition of a number to a string");
                else if ((numberA == null) && (numberB == null))
                {
                    return new MString(strA.Value + strB.Value);
                }
                else
                {
                    switch (numberA.TypeName)
                    {
                        case MType.M_INTEGER_TYPENAME:
                            {
                                MFraction fraction = numberB as MFraction;
                                if (fraction != null)
                                {
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
                                MFraction fraction = numberA as MFraction;
                                fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                                long numerator, denominator;
                                MFraction otherFraction = numberB as MFraction;
                                if (otherFraction == null)
                                    Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator, out denominator);
                                else
                                {
                                    numerator = otherFraction.Numerator;
                                    denominator = otherFraction.Denominator;
                                }
                                fraction.Add(numerator, denominator);
                                return fraction;
                            }
                    }
                }

                return null;
            }

            public static MType Subtract(Interpreter interpreter, MType a, MType b)
            {
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            MFraction rhsFraction = numberB as MFraction;
                            if (rhsFraction != null)
                            {
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
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction == null)
                                Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator, out denominator);
                            else
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            fraction.Subtract(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            public static MType Multiply(Interpreter interpreter, MType a, MType b)
            {
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            MFraction fraction = numberB as MFraction;
                            if (fraction != null)
                            {
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
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction == null)
                                Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator, out denominator);
                            else
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            fraction.Multiply(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            public static MType Divide(Interpreter interpreter, MType a, MType b)
            {
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        {
                            MFraction rhsFraction = numberB as MFraction;
                            if (rhsFraction != null)
                            {
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
                            MFraction fraction = numberA as MFraction;
                            fraction = new MFraction(fraction.Numerator, fraction.Denominator);
                            long numerator, denominator;
                            MFraction otherFraction = numberB as MFraction;
                            if (otherFraction == null)
                                Operations.Misc.DoubleToFraction(numberB.MRealValue, out numerator, out denominator);
                            else
                            {
                                numerator = otherFraction.Numerator;
                                denominator = otherFraction.Denominator;
                            }
                            fraction.Divide(numerator, denominator);
                            return fraction;
                        }
                }

                return null;
            }

            public static MType Pow(Interpreter interpreter, MType a, MType b)
            {
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                switch (numberA.TypeName)
                {
                    case MType.M_INTEGER_TYPENAME:
                        return new MInteger((long)System.Math.Round(System.Math.Pow(numberA.MRealValue, numberB.MRealValue)));
                    case MType.M_REAL_TYPENAME:
                        return new MReal(System.Math.Pow(numberA.MRealValue, numberB.MRealValue));
                    case MType.M_FRACTION_TYPENAME:
                        MFraction fraction = numberA as MFraction;
                        long numerator = (long)System.Math.Round(System.Math.Pow(fraction.Numerator, numberB.MRealValue));
                        long denominator = (long)System.Math.Round(System.Math.Pow(fraction.Denominator, numberB.MRealValue));
                        return new MFraction(numerator, denominator);
                }

                return null;
            }

            public static MType Modulus(Interpreter interpreter, MType value)
            {
                MNumber number;
                MException exception = AssignNumberValue(interpreter, out number, value);
                if (exception != null) return exception;

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

            public static MType Inequality(Interpreter interpreter, MType a, MType b, string inequality)
            {
                MNumber numberA, numberB;
                MException exception = AssignNumberValue(interpreter, out numberA, a);
                if (exception != null) return exception;
                exception = AssignNumberValue(interpreter, out numberB, b);
                if (exception != null) return exception;

                bool result = false;
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
                }

                return new MBoolean(result);
            }
        }

        public static MType Equal(Interpreter interpreter, MType a, MType b, bool strict)
        {
            if (a is MVariable) a = ((MVariable)a).Value;
            if (b is MVariable) b = ((MVariable)b).Value;
            if ((a.TypeName != b.TypeName) && (!(a is MNumber) || !(b is MNumber)))
                return new MException(interpreter, "Type '" + a.TypeName + "' cannot be compared with type '" + b.TypeName + "'");

            if (a is MNumber)
            {
                bool result;
                if (strict) result = (((MNumber)a).MRealValue == ((MNumber)b).MRealValue);
                else result = (((MNumber)a).MIntegerValue == ((MNumber)b).MIntegerValue);

                return new MBoolean(result);
            }
            else if (a is MBoolean)
            {
                return new MBoolean(((MBoolean)a).Value == ((MBoolean)b).Value);
            }
            else if (a is MString) {
                return new MBoolean(((MString)a).Value == ((MString)b).Value);
            }
            else if (a is MVariable)
            {
                return new MBoolean(a == b);
            }
            else if (a is MFunction)
            {
                MFunction funcA = a as MFunction, funcB = b as MFunction;
                return new MBoolean((funcA.HardCodedFunction == funcB.HardCodedFunction) && (funcA.CustomFunction == funcB.CustomFunction));
            }
            else if (a is MNil)
            {
                return new MBoolean(b is MNil);
            }

            return null;
        }

        public static MType NotEqual(Interpreter interpreter, MType a, MType b)
        {
            MType value = Equal(interpreter, a, b, true);
            MBoolean result = value as MBoolean;
            if (result == null) return value;
            result.Value = !result.Value;
            return result;
        }

        public class Misc
        {
            public static void DoubleToFraction(double value, out long numerator, out long denominator)
            // Algorithm taken from http://homepage.smc.edu/kennedy_john/DEC2FRAC.PDF by John Kennedy
            {
                const double precision = 0.000005;

                double sign, z, previousDenominator, scratchValue;

                if (value < 0.0) sign = -1.0;
                else sign = 1.0;
                value = System.Math.Abs(value);

                if (value == (long)value)
                {
                    numerator = (long)(value * sign);
                    denominator = 1;
                    return;
                }

                if (value < 1.0e-18)
                {
                    numerator = (long)sign;
                    denominator = 999999999999999999;
                    return;
                }

                if (value > 1.0e+19)
                {
                    numerator = (long)(999999999999999999 * sign);
                    denominator = 1;
                    return;
                }

                z = value;
                previousDenominator = 0.0;
                denominator = 1;

                do
                {
                    z = 1.0 / (z - (long)z);
                    scratchValue = denominator;
                    denominator = (long)((denominator * (long)z) + previousDenominator);
                    previousDenominator = scratchValue;
                    numerator = (long)((value * (double)denominator) + 0.5);
                } while (!((System.Math.Abs(value - ((double)numerator / (double)denominator)) < precision) || (z == (long)z)));
            }
        }
    }
}
