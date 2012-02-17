using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types.Singletons
{
    /// <summary>
    /// A class used to indicate an infinite value. Not actually used anywhere.
    /// </summary>
    class TInfinity : TType
    {
        static readonly TInfinity instance = new TInfinity();
        public static TInfinity Instance { get { return instance; } }

        public override string TypeName { get { return T_INFINITY_TYPENAME; } }

        public override string ToCSString()
        {
            return "infinity";
        }

        private TInfinity() {}
    }
}
