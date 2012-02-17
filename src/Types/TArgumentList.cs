using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType which is a list of TType arguments that are passed to a Toast function.
    /// </summary>
    class TArgumentList : TType
    {
        List<TType> arguments;

        public int Count { get { return arguments.Count; } }

        public TType this[int index]
        {
            get { return arguments[index]; }
            set { arguments[index] = value; }
        }

        public TArgumentList()
        {
            arguments = new List<TType>();
        }

        /// <summary>
        /// Appends a TType to this TArgumentList. If a TArgumentList is passed, then the values contained in that
        /// TArgumentList are appended to this TArgumentList. If the values to be appended are TVariables, then the
        /// value of the TVariable will be appended instead of the TVariable itself (although if TVariable A
        /// containing TVariable B is passed, then TVariable B will be appended, as opposed to TVariable B's value).
        /// </summary>
        /// <param name="argument">The TType to append.</param>
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

        public override string TypeName { get { return T_ARGUMENTLIST_TYPENAME; } }

        public override string ToCSString()
        {
            StringBuilder returnString = new StringBuilder();
            for (int i = 0; i < arguments.Count; ++i)
            {
                returnString.Append(arguments[i].ToCSString());
                if (i < arguments.Count - 1) returnString.Append(", ");
            }
            return returnString.ToString();
        }
    }
}
