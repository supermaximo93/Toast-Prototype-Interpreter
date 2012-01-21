using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types.Singletons
{
    class MInfinity : MType
    {
        private static readonly MInfinity instance = new MInfinity();
        public static MInfinity Instance { get { return instance; } }

        public override string TypeName { get { return M_INFINITY_TYPENAME; } }

        public override string ToCSString()
        {
            return "infinity";
        }
    }
}
