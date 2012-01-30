using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types;
using Toast.Types.Singletons;

namespace Toast
{
    /// <summary>
    /// A static class containing the implementations of the language's standard library functions
    /// </summary>
    static class StdLibrary
    {
        private static Dictionary<string, TFunction> functions;

        /// <summary>
        /// Loads up the functions of the standard library into the function dictionary.
        /// </summary>
        public static void Init()
        {
            if (functions != null) return; // Functions already loaded, so no need to continue

            functions = new Dictionary<string, TFunction>();
            functions.Add(PRINT_NAME, new TFunction(PRINT_NAME, Print, null, null));
            functions.Add(READ_NAME, new TFunction(READ_NAME, Read, new string[0], null));
            functions.Add(READSTRING_NAME, new TFunction(READSTRING_NAME, ReadString, new string[0], null));
            functions.Add(EXIT_NAME,
                new TFunction(EXIT_NAME, Exit, new string[] { "return_value" }, new TType[] { TNil.Instance }));
            functions.Add(RANDOM_NAME, new TFunction(RANDOM_NAME, Random, new string[] { "maximum" }, null));
            functions.Add(LOAD_NAME, new TFunction(LOAD_NAME, Load, new string[] { "file" }, null));

            randomNumberGenerator = new System.Random(DateTime.Now.Millisecond); // Required for Random method
        }

        /// <summary>
        /// Searches for a TFunction with a specified name.
        /// </summary>
        /// <param name="functionName">The identifier of the function to search for.</param>
        /// <returns>The TFunction being searched for. Returns null if the function could not be found.</returns>
        public static TFunction GetFunction(string functionName)
        {
            TFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return null;
        }
       
        /// <summary>
        /// The method that represents the 'load' function in the language.
        /// Loads a script file with the specified file name (given by a TString) and runs it.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>TNil</returns>
        public static TType Load(Interpreter interpreter, TArgumentList args)
        {
            TString fileName = args[0] as TString;
            if (fileName == null) return new TException(interpreter, "Name of file to load must be given as a string");
            interpreter.LoadFile(fileName.Value);
            return TNil.Instance;
        }
        public const string LOAD_NAME = "load";

        /// <summary>
        /// The method that represents the 'print' function in the language.
        /// Takes any number of values and writes their string values.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>TNil</returns>
        public static TType Print(Interpreter interpreter, TArgumentList args)
        {
            // Loop through arguments, append their string values to a StringBuilder, and output the StringBuilder
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < args.Count; ++i)
            {
                TString arg = args[i] as TString;
                if (arg == null) str.Append(args[i].ToCSString());
                else str.Append(arg.Value);
            }
            System.Console.WriteLine(str);

            return TNil.Instance;
        }
        public const string PRINT_NAME = "print";

        /// <summary>
        /// The method that represents the 'read' function in the language.
        /// Reads in a string and converts it into the most suitable TType.
        /// If the string cannot be converted, it just returns the string as a TString.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>An TType of the type that the entered string represents best.</returns>
        public static TType Read(Interpreter interpreter, TArgumentList args)
        {
            // Read in a string and convert it to a suitable TType with TType.Parse.
            string str = System.Console.ReadLine();
            TType value = TType.Parse(interpreter, str);
            if (value is TException) value = new TString(str); // If the string can't be parsed, return it as a TString
            return value;
        }
        public const string READ_NAME = "read";

        /// <summary>
        /// The method that represents the 'read_string' function in the language.
        /// Reads in a string returns it as a TString.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>An TString representing the string that was entered.</returns>
        public static TType ReadString(Interpreter interpreter, TArgumentList args)
        {
            return new TString(System.Console.ReadLine());
        }
        public const string READSTRING_NAME = "read_string";

        /// <summary>
        /// The method that represents the 'exit' function in the language.
        /// Exits from the current function, returning any value passed to the function.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>The argument passed to the function. If no arguments were passed, it returns TNil.</returns>
        public static TType Exit(Interpreter interpreter, TArgumentList args)
        {
            // Get the argument passed (if any) to return
            TType returnValue = TNil.Instance;
            if (args.Count > 0) returnValue = args[0];

            // Pops from the function stack or kill the interpreter if the stack level is already at it's lowest
            // When this method returns, the calling method should check the stack level and exit accordingly
            if (interpreter.Stack.Level <= 1) interpreter.Kill();
            else interpreter.Stack.Pop();

            return returnValue;
        }
        public const string EXIT_NAME = "exit";

        /// <summary>
        /// The method that represents the 'random' function in the language.
        /// Generates a random TInteger between 0 and the given value - 1.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as a TArgumentList.</param>
        /// <returns>An TInteger with a value between 0 and the given value - 1</returns>
        public static TType Random(Interpreter interpreter, TArgumentList args)
        {
            // Check if the argument passed is a number, and return a new TInteger from the random value generated
            TNumber maximum = args[0] as TNumber;
            if (maximum == null) return new TException(interpreter,
                "Arguments of type '" + args[0].TypeName + "' cannot be used by random function");

            return new TInteger(randomNumberGenerator.Next((int)maximum.TIntegerValue));
        }
        static System.Random randomNumberGenerator;
        public const string RANDOM_NAME = "random";
    }
}
