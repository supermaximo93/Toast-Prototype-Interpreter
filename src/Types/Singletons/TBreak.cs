using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types.Singletons
{
    class TBreak : TType
    {
        private static readonly TBreak instance = new TBreak();
        public static TBreak Instance { get { return instance; } }

        public override string TypeName { get { return T_BREAK_TYPENAME; } }

        public override string ToCSString()
        {
            return "break";
        }
    }
}
