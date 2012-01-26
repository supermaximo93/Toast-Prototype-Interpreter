using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TBoolean : TType
    {
        private bool value;
        public bool Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public TBoolean()
        {
            value = false;
        }

        public TBoolean(bool value)
        {
            this.value = value;
        }

        public TBoolean(string value)
        {
            this.value = (value.ToLower() == "yes");
        }

        public override string TypeName { get { return T_BOOLEAN_TYPENAME; } }

        public override string ToCSString()
        {
            return value ? "yes" : "no";
        }

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
