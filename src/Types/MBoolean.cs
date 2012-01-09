using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MBoolean : MType
    {
        private bool value;
        public bool Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public MBoolean()
        {
            value = false;
        }

        public MBoolean(bool value)
        {
            this.value = value;
        }

        public MBoolean(string value)
        {
            this.value = (value.ToLower() == "yes");
        }

        public override string TypeName { get { return M_BOOLEAN_TYPENAME; } }

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
