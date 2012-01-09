using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MArgumentList : MType
    {
        private List<MType> arguments;

        public MArgumentList()
        {
            arguments = new List<MType>();
        }

        public void Add(MType argument)
        {
            MArgumentList argList = argument as MArgumentList;
            if (argList == null)
            {
                MVariable variable = argument as MVariable;
                if (variable == null) arguments.Add(argument);
                else arguments.Add(variable.Value);
            }
            else
            {
                for (int i = 0; i < argList.Count; ++i) arguments.Add(argList[i]);
            }     
        }

        public int Count { get { return arguments.Count; } }

        public MType this[int index]
        {
            get { return arguments[index]; }
            set { arguments[index] = value; }
        }

        public override string TypeName { get { return M_ARGUMENTLIST_TYPENAME; } }

        public override string ToCSString()
        {
            string returnString = "{ ";
            for (int i = 0; i < arguments.Count; ++i)
            {
                returnString += arguments[i].ToCSString();
                if (i < arguments.Count - 1) returnString += ", ";
            }
            return returnString + " }";
        }
    }
}
