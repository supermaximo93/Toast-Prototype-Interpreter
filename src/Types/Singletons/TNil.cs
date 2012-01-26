using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types.Singletons
{
    class TNil : TType
    {
        private static readonly TNil instance = new TNil();
        public static TNil Instance { get { return instance; } }

        public override string TypeName { get { return T_NIL_TYPENAME; } }

        public override string ToCSString()
        {
            return "nil";
        }
    }
}
