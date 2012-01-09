using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types;

namespace MathsLanguage
{
    class StdLibrary
    {
        private static Dictionary<string, MFunction> functions;

        public static void Init()
        {
            if (functions != null) return;

            functions = new Dictionary<string, MFunction>();
            functions.Add(PRINT_NAME, new MFunction(PRINT_NAME, Print, null, null));
            functions.Add(READ_NAME, new MFunction(READ_NAME, Read, new string[0], null));
        }

        public static MFunction GetFunction(string functionName)
        {
            MFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return null;
        }

        public const string PRINT_NAME = "print";
        public static MType Print(Interpreter interpreter, MArgumentList args)
        {
            string str = "";
            for (int i = 0; i < args.Count; ++i)
            {
                MString arg = args[i] as MString;
                if (arg == null) str += args[i].ToCSString();
                else str += arg.Value;
            }
            System.Console.WriteLine(str);
            return new MNil();
        }

        public const string READ_NAME = "read";
        public static MType Read(Interpreter interpreter, MArgumentList args)
        {
            return MType.Parse(interpreter, System.Console.ReadLine());
        }
    }
}
