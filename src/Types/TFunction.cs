using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types.Singletons;

namespace Toast.Types
{
    /// <summary>
    /// A TType representing a function. Can contain a function as a hardcoded C# method, a string of Toast code
    /// or a TBlock.
    /// </summary>
    class TFunction : TType
    {
        public delegate TType function(Interpreter interpreter, TArgumentList args);
        static Dictionary<string, TFunction> functions;

        /// <summary>
        /// Initialises the function dictionaries and loads the Toast standard library functions.
        /// </summary>
        public static void Init()
        {
            if (functions != null) return;
            functions = new Dictionary<string, TFunction>();

            StdLibrary.Init();
        }

        /// <summary>
        /// Searches for a Toast function with the specified name.
        /// </summary>
        /// <param name="functionName">The name of the Toast function to seach for.</param>
        /// <param name="includeStdLibrary">Whether the Toast standard library should be searched.</param>
        /// <returns>A TFunction if the operation was successful, otherwise null.</returns>
        public static TFunction GetFunction(string functionName, bool includeStdLibrary = true)
        {
            TFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return includeStdLibrary ? StdLibrary.GetFunction(functionName) : null;
        }

        /// <summary>
        /// Adds a TFunction to the function dictionary.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="function">The TFunction to be added to the function dictionary.</param>
        /// <returns>Null if the operation was successful, otherwise a TException.</returns>
        public static TException AddFunction(Interpreter interpreter, TFunction function)
        {
            if (function == null) return new TException(interpreter, "Null TFunction given");

            // Reject the function if it's already in the standard library
            TFunction existingFunction = StdLibrary.GetFunction(function.Name);
            if (existingFunction != null)
                return new TException(interpreter,
                    "Standard library function with name '" + function.Name + "' already exists");

            // Add it to the function dictionary, overwriting another function with the same name if neccessary
            existingFunction = GetFunction(function.Name, false);
            if (existingFunction == null) functions.Add(function.Name, function);
            else
            {
                existingFunction.HardCodedFunction = null;
                existingFunction.CustomFunction = "";
                existingFunction.Block = null;
                existingFunction.CopyFrom(function);
            }

            return null;
        }

        public string Name { get; private set; }
        public string[] ArgNames { get; private set; }
        public int ArgCount { get { return ArgNames == null ? -1 : ArgNames.Length; } }

        public TType[] DefaultArgs { get; private set; }

        public function HardCodedFunction { get; private set; }
        public string CustomFunction { get; private set; }
        public TBlock Block { get; private set; }

        /// <summary>
        /// A helper method used by TFunction constructors to copy argument names and argument default values passed
        /// to them.
        /// </summary>
        void CopyArguments(string[] argNames, TType[] defaultArgs)
        {
            if (argNames == null)
            {
                ArgNames = null;
                DefaultArgs = null;
            }
            else
            {
                ArgNames = new string[argNames.Length];
                if (argNames.Length > 0) argNames.CopyTo(ArgNames, 0);

                if (defaultArgs == null) DefaultArgs = new TType[argNames.Length];
                else
                {
                    DefaultArgs = new TType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(DefaultArgs, 0);
                }
            }
        }

        // Someone tell me if there's a better way to document three similar but slightly different methods...

        /// <summary>
        /// A TFunction constructor.
        /// </summary>
        /// <param name="name">The name of the new Toast function.</param>
        /// <param name="function">The C# method which is called when the Toast function is called.</param>
        /// <param name="argNames">
        /// The argument names to use in the function. Pass null for the function to use a variable number of
        /// arguments.
        /// </param>
        /// <param name="defaultArgs">
        /// The default values of arguments, as TTypes. Use null in the array to specify that no default value should
        /// be used for a particular argument, or pass null to indicate that no arguments should have default values.
        /// </param>
        public TFunction(string name, function function, string[] argNames, TType[] defaultArgs)
        {
            Name = name;
            HardCodedFunction = function;
            CustomFunction = "";
            Block = null;
            CopyArguments(argNames, defaultArgs);
        }

        /// <summary>
        /// A TFunction constructor.
        /// </summary>
        /// <param name="name">The name of the new Toast function.</param>
        /// <param name="function">A string of Toast code to be executed when the Toast function is called.</param>
        /// <param name="argNames">
        /// The argument names to use in the function. Pass null for the function to use a variable number of
        /// arguments.
        /// </param>
        /// <param name="defaultArgs">
        /// The default values of arguments, as TTypes. Use null in the array to specify that no default value should
        /// be used for a particular argument, or pass null to indicate that no arguments should have default values.
        /// </param>
        public TFunction(string name, string function, string[] argNames, TType[] defaultArgs)
        {
            Name = name;
            HardCodedFunction = null;
            CustomFunction = function;
            Block = null;
            CopyArguments(argNames, defaultArgs);
        }

        /// <summary>
        /// A TFunction constructor.
        /// </summary>
        /// <param name="name">The name of the new Toast function.</param>
        /// <param name="function">A TBlock to be executed when the Toast function is called.</param>
        /// <param name="argNames">
        /// The argument names to use in the function. Pass null for the function to use a variable number of
        /// arguments.
        /// </param>
        /// <param name="defaultArgs">
        /// The default values of arguments, as TTypes. Use null in the array to specify that no default value should
        /// be used for a particular argument, or pass null to indicate that no arguments should have default values.
        /// </param>
        public TFunction(string name, TBlock block, string[] argNames, TType[] defaultArgs)
        {
            Name = name;
            Block = block;
            HardCodedFunction = null;
            CustomFunction = "";
            CopyArguments(argNames, defaultArgs);
        }

        /// <summary>
        /// A copy constructor or TFunction.
        /// </summary>
        /// <param name="function">The existing TFunction whose data is to be copied into the new TFunction.</param>
        public TFunction(TFunction function)
        {
            CopyFrom(function);
        }

        /// <summary>
        /// Calls the TFunction with the specified arguments.
        /// </summary>
        /// <param name="interpreter">The interpreter that the method is being called from.</param>
        /// <param name="value">
        /// The argument to pass to the function. When passing multiple arguments, use a TArgumentList.
        /// </param>
        /// <returns></returns>
        public TType Call(Interpreter interpreter, TType value)
        {
            // If value is already a TArgumentList, then simply copy the reference, otherwise create a new
            // TArgumentList and add the value to it. This TArgument list is to be passed to the function.
            TArgumentList argList = value as TArgumentList;
            if (argList == null)
            {
                argList = new TArgumentList();
                argList.Add(value);
            }

            // If the function takes a fixed number of arguments...
            if (ArgNames != null)
            {
                // Occupy the argument list with the default arguments. If there is no default argument in a place
                // where an argument should have been given, return a TException.
                if (argList.Count < ArgNames.Length)
                {
                    for (int i = argList.Count; i < DefaultArgs.Length; ++i)
                    {
                        if (DefaultArgs[i] == null) break;
                        else argList.Add(DefaultArgs[i]);
                    }
                }

                if (argList.Count != ArgNames.Length)
                {
                    return new TException(interpreter, "Incorrect number of arguments for function '" + Name + "'",
                        argList.Count.ToString() + " out of " + ArgNames.Length.ToString() + " given");
                }
            }

            interpreter.Stack.Push();
            // Keep a track of the new stack level so that if a function calls 'exit()', which pops from the stack,
            // this will be able to be detected and the function call can be terminated properly
            int stackLevel = interpreter.Stack.Level;

            // Put the arguments on the current stack 'frame'
            if (ArgNames == null)
            {
                for (int i = 0; i < argList.Count; ++i)
                    interpreter.Stack.AddVariable(new TVariable("arg" + i.ToString(), argList[i]));
            }
            else
            {
                for (int i = 0; i < argList.Count; ++i)
                    interpreter.Stack.AddVariable(new TVariable(ArgNames[i], argList[i]));
            }

            TType returnValue = null;
            bool dontPop = false; // Set to true if the stack is popped during the call, i.e. if 'exit()' is called

            // Call the function
            if (HardCodedFunction != null) returnValue = HardCodedFunction.Invoke(interpreter, argList);
            else if (Block != null)
            {
                bool breakUsed;
                returnValue = Block.Execute(interpreter, out dontPop, out breakUsed);
            }
            else if (CustomFunction != "")
            {
                returnValue = interpreter.Interpret(CustomFunction, true);
                if (interpreter.Stack.Level < stackLevel) dontPop = true;
            }

            // If returnValue is a TVariable, then return the value of the TVariable (e.g. we want to return 5, as
            // opposed to the variable X which contains 5)
            TVariable variable = returnValue as TVariable;
            if (variable != null) returnValue = variable.Value;

            if (!dontPop) interpreter.Stack.Pop();

            return returnValue ?? TNil.Instance;
        }

        /// <summary>
        /// Copies the data from another TFunction into this TFunction.
        /// </summary>
        /// <param name="function">The existing TFunction whose data is to be copied into the new TFunction.</param>
        public void CopyFrom(TFunction function)
        {
            Name = function.Name;
            HardCodedFunction = function.HardCodedFunction;
            CustomFunction = function.CustomFunction;
            Block = function.Block;
            CopyArguments(function.ArgNames, function.DefaultArgs);
        }

        public override string TypeName { get { return "function"; } }

        public override string ToCSString()
        {
            return Name;
        }
    }
}
