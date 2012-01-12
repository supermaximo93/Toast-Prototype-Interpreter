using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types.Singletons
{
    class MBreak : MType
    {
        private static readonly MBreak instance = new MBreak();
        public static MBreak Instance { get { return instance; } }

        public override string TypeName { get { return M_BREAK_TYPENAME; } }

        public override string ToCSString()
        {
            return "break";
        }
    }
}
