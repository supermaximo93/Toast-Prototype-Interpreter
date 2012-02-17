using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType representing a boolean value, stored as a bool.
    /// </summary>
    class TBoolean : TType
    {
        public bool Value { get; set; }

        public TBoolean()
        {
            Value = false;
        }

        public TBoolean(bool value)
        {
            Value = value;
        }

        public TBoolean(string value)
        {
            Value = (value.ToLower() == "yes");
        }

        public override string TypeName { get { return T_BOOLEAN_TYPENAME; } }

        public override string ToCSString()
        {
            return Value ? "yes" : "no";
        }

        /// <summary>
        /// Attempts to convert the given string to a bool.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <param name="result">The variable that will be set to the value of the converted string.</param>
        /// <returns>True if the operation was successful</returns>
        public static bool TryParse(string value, out bool result)
        {
            value = value.ToLower();
            if (value == "yes")
            {
                result = true;
                return true;
            }
            if (value == "no")
            {
                result = false;
                return true;
            }
            return result = false;
        }
    }
}
