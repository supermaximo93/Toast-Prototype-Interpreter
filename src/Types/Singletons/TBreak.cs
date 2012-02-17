using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types.Singletons
{
    /// <summary>
    /// A class used as an indicator that a loop should be broken. It's kinda hackish and shoundn't be used to break
    /// out of functions, although it can be used for that purpose (use 'exit()' instead).
    /// </summary>
    class TBreak : TType
    {
        static readonly TBreak instance = new TBreak();
        public static TBreak Instance { get { return instance; } }

        public override string TypeName { get { return T_BREAK_TYPENAME; } }

        public override string ToCSString()
        {
            return "break";
        }

        private TBreak() {}
    }
}
