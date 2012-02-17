using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast.Types
{
    /// <summary>
    /// A TType which a list of parameter names that are used in the declaration of a function.
    /// </summary>
    class TParameterList : TType
    {
        private List<string> parameters;
        public string[] ParameterNames { get { return parameters.ToArray(); } }

        public int Count { get { return parameters.Count; } }

        public string this[int index]
        {
            get { return parameters[index]; }
            set { parameters[index] = value; }
        }

        public TParameterList()
        {
            parameters = new List<string>();
        }

        /// <summary>
        /// Appends the contents of another TParameterList to this TParameterList.
        /// </summary>
        /// <param name="parameterList">The TParameterList to append.</param>
        public void Add(TParameterList parameterList)
        {
            for (int i = 0; i < parameterList.Count; ++i) parameters.Add(parameterList[i]);
        }

        /// <summary>
        /// Appends a parameter name to this TParameterList.
        /// </summary>
        /// <param name="parameterName">The parameter name to append to this TParameterList</param>
        public void Add(string parameterName)
        {
            parameters.Add(parameterName);
        }

        public override string TypeName { get { return T_PARAMETERLIST_TYPENAME; } }

        public override string ToCSString()
        {
            StringBuilder returnString = new StringBuilder();
            for (int i = 0; i < parameters.Count; ++i)
            {
                returnString.Append(parameters[i]);
                if (i < parameters.Count - 1) returnString.Append(", ");
            }
            return returnString.ToString();
        }
    }
}
