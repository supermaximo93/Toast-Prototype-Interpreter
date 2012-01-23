using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types;
using MathsLanguage.Types.Singletons;

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
            functions.Add(READSTRING_NAME, new MFunction(READSTRING_NAME, ReadString, new string[0], null));
            functions.Add(EXIT_NAME, new MFunction(EXIT_NAME, Exit, new string[] { "return_value" }, new MType[] { MNil.Instance }));
            functions.Add(RANDOM_NAME, new MFunction(RANDOM_NAME, Random, new string[] { "maximum" }, null));
            functions.Add(LOAD_NAME, new MFunction(LOAD_NAME, Load, new string[] { "file" }, null));

            randomNumberGenerator = new System.Random(DateTime.Now.Millisecond);
        }

        public static MFunction GetFunction(string functionName)
        {
            MFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return null;
        }

        public const string LOAD_NAME = "load";
        public static MType Load(Interpreter interpreter, MArgumentList args)
        {
            MString fileName = args[0] as MString;
            if (fileName == null) return new MException(interpreter, "Name of file to load must be given as a string");
            interpreter.LoadFile(fileName.Value);
            return MNil.Instance;
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
            return MNil.Instance;
        }

        public const string READ_NAME = "read";
        public static MType Read(Interpreter interpreter, MArgumentList args)
        {
            string str = System.Console.ReadLine();
            MType value = MType.Parse(interpreter, str);
            if (value is MException) value = MType.Parse(interpreter, '"' + str + '"');
            return value;
        }

        public const string READSTRING_NAME = "read_string";
        public static MType ReadString(Interpreter interpreter, MArgumentList args)
        {
            return new MString(System.Console.ReadLine());
        }

        public const string EXIT_NAME = "exit";
        public static MType Exit(Interpreter interpreter, MArgumentList args)
        {
            MType returnValue = null;
            if (args.Count > 0) returnValue = args[0];

            if (interpreter.Stack.Level <= 1) interpreter.Kill();
            else interpreter.Stack.Pop();

            return returnValue ?? MNil.Instance;
        }

        static System.Random randomNumberGenerator;
        public const string RANDOM_NAME = "random";
        public static MType Random(Interpreter interpreter, MArgumentList args)
        {
            MNumber maximum = args[0] as MNumber;
            if (maximum == null)
                return new MException(interpreter, "Arguments of type '" + args[0].TypeName + "' cannot be used by random function");

            return new MInteger(randomNumberGenerator.Next((int)maximum.MIntegerValue));
        }
    }
}
