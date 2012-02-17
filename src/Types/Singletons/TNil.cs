using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types.Singletons
{
    /// <summary>
    /// A class used to represent a null value in Toast.
    /// </summary>
    class TNil : TType
    {
        static readonly TNil instance = new TNil();
        public static TNil Instance { get { return instance; } }

        public override string TypeName { get { return T_NIL_TYPENAME; } }

        public override string ToCSString()
        {
            return "nil";
        }

        private TNil() {}
    }
}
