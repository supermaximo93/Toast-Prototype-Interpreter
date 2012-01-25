using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathsLanguage.Types;
using MathsLanguage.Types.Singletons;

namespace MathsLanguage
{
    /// <summary>
    /// A static class containing the implementations of the language's standard library functions
    /// </summary>
    class StdLibrary
    {
        private static Dictionary<string, MFunction> functions;

        /// <summary>
        /// Loads up the functions of the standard library into the function dictionary.
        /// </summary>
        public static void Init()
        {
            if (functions != null) return; // Functions already loaded, so no need to continue

            functions = new Dictionary<string, MFunction>();
            functions.Add(PRINT_NAME, new MFunction(PRINT_NAME, Print, null, null));
            functions.Add(READ_NAME, new MFunction(READ_NAME, Read, new string[0], null));
            functions.Add(READSTRING_NAME, new MFunction(READSTRING_NAME, ReadString, new string[0], null));
            functions.Add(EXIT_NAME,
                new MFunction(EXIT_NAME, Exit, new string[] { "return_value" }, new MType[] { MNil.Instance }));
            functions.Add(RANDOM_NAME, new MFunction(RANDOM_NAME, Random, new string[] { "maximum" }, null));
            functions.Add(LOAD_NAME, new MFunction(LOAD_NAME, Load, new string[] { "file" }, null));

            randomNumberGenerator = new System.Random(DateTime.Now.Millisecond); // Required for Random method
        }

        /// <summary>
        /// Searches for an MFunction with a specified name.
        /// </summary>
        /// <param name="functionName">The identifier of the function to search for.</param>
        /// <returns>The MFunction being searched for. Returns null if the function could not be found.</returns>
        public static MFunction GetFunction(string functionName)
        {
            MFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return null;
        }
       
        /// <summary>
        /// The method that represents the 'load' function in the language.
        /// Loads a script file with the specified file name (given by an MString) and runs it.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>MNil</returns>
        public static MType Load(Interpreter interpreter, MArgumentList args)
        {
            MString fileName = args[0] as MString;
            if (fileName == null) return new MException(interpreter, "Name of file to load must be given as a string");
            interpreter.LoadFile(fileName.Value);
            return MNil.Instance;
        }
        public const string LOAD_NAME = "load";

        /// <summary>
        /// The method that represents the 'print' function in the language.
        /// Takes any number of values and writes their string values.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>MNil</returns>
        public static MType Print(Interpreter interpreter, MArgumentList args)
        {
            // Loop through arguments, append their string values to a StringBuilder, and output the StringBuilder
            StringBuilder str = new StringBuilder();
            for (int i = 0; i < args.Count; ++i)
            {
                MString arg = args[i] as MString;
                if (arg == null) str.Append(args[i].ToCSString());
                else str.Append(arg.Value);
            }
            System.Console.WriteLine(str);

            return MNil.Instance;
        }
        public const string PRINT_NAME = "print";

        /// <summary>
        /// The method that represents the 'read' function in the language.
        /// Reads in a string and converts it into the most suitable MType.
        /// If the string cannot be converted, it just returns the string as an MString.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>An MType of the type that the entered string represents best.</returns>
        public static MType Read(Interpreter interpreter, MArgumentList args)
        {
            // Read in a string and convert it to a suitable MType with MType.Parse.
            string str = System.Console.ReadLine();
            MType value = MType.Parse(interpreter, str);
            if (value is MException) value = new MString(str); // If the string can't be parsed, return it as an MString
            return value;
        }
        public const string READ_NAME = "read";

        /// <summary>
        /// The method that represents the 'read_string' function in the language.
        /// Reads in a string returns it as an MString.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>An MString representing the string that was entered.</returns>
        public static MType ReadString(Interpreter interpreter, MArgumentList args)
        {
            return new MString(System.Console.ReadLine());
        }
        public const string READSTRING_NAME = "read_string";

        /// <summary>
        /// The method that represents the 'exit' function in the language.
        /// Exits from the current function, returning any value passed to the function.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>The argument passed to the function. If no arguments were passed, it returns MNil.</returns>
        public static MType Exit(Interpreter interpreter, MArgumentList args)
        {
            // Get the argument passed (if any) to return
            MType returnValue = MNil.Instance;
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
        /// Generates a random MInteger between 0 and the given value - 1.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="args">The arguments being passed to the function as an MArgumentList.</param>
        /// <returns>An MInteger with a value between 0 and the given value - 1</returns>
        public static MType Random(Interpreter interpreter, MArgumentList args)
        {
            // Check if the argument passed is a number, and return a new MInteger from the random value generated
            MNumber maximum = args[0] as MNumber;
            if (maximum == null) return new MException(interpreter,
                "Arguments of type '" + args[0].TypeName + "' cannot be used by random function");

            return new MInteger(randomNumberGenerator.Next((int)maximum.MIntegerValue));
        }
        static System.Random randomNumberGenerator;
        public const string RANDOM_NAME = "random";
    }
}
