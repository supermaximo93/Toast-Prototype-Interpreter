using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    class TArgumentList : TType
    {
        private List<TType> arguments;

        public TArgumentList()
        {
            arguments = new List<TType>();
        }

        public void Add(TType argument)
        {
            TArgumentList argList = argument as TArgumentList;
            if (argList == null)
            {
                TVariable variable = argument as TVariable;
                if (variable == null) arguments.Add(argument);
                else arguments.Add(variable.Value);
            }
            else
            {
                for (int i = 0; i < argList.Count; ++i) arguments.Add(argList[i]);
            }     
        }

        public int Count { get { return arguments.Count; } }

        public TType this[int index]
        {
            get { return arguments[index]; }
            set { arguments[index] = value; }
        }

        public override string TypeName { get { return T_ARGUMENTLIST_TYPENAME; } }

        public override string ToCSString()
        {
            string returnString = "";
            for (int i = 0; i < arguments.Count; ++i)
            {
                returnString += arguments[i].ToCSString();
                if (i < arguments.Count - 1) returnString += ", ";
            }
            return returnString;
        }
    }
}
