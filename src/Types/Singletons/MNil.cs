using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types.Singletons
{
    class MNil : MType
    {
        private static readonly MNil instance = new MNil();
        public static MNil Instance { get { return instance; } }

        public override string TypeName { get { return M_NIL_TYPENAME; } }

        public override string ToCSString()
        {
            return "nil";
        }
    }
}
