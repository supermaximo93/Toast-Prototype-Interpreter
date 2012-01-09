using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MNil : MType
    {
        public override string TypeName { get { return M_NIL_TYPENAME; } }

        public override string ToCSString()
        {
            return "nil";
        }
    }
}
