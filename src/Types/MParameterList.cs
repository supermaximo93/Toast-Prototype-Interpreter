using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MParameterList : MType
    {
        private List<string> parameters;
        public string[] ParameterNames { get { return parameters.ToArray(); } }

        public MParameterList()
        {
            parameters = new List<string>();
        }

        public void Add(MParameterList parameterList)
        {
            for (int i = 0; i < parameterList.Count; ++i) parameters.Add(parameterList[i]);
        }

        public void Add(string parameterName)
        {
            parameters.Add(parameterName);
        }

        public int Count { get { return parameters.Count; } }

        public string this[int index]
        {
            get { return parameters[index]; }
            set { parameters[index] = value; }
        }

        public override string TypeName { get { return M_PARAMETERLIST_TYPENAME; } }

        public override string ToCSString()
        {
            string returnString = "";
            for (int i = 0; i < parameters.Count; ++i)
            {
                returnString += parameters[i];
                if (i < parameters.Count - 1) returnString += ", ";
            }
            return returnString;
        }
    }
}
