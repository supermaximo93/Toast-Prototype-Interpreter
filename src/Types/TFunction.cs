using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Toast.Types.Singletons;

namespace Toast.Types
{
    class TFunction : TType
    {
        public delegate TType function(Interpreter interpreter, TArgumentList args);
        private static Dictionary<string, TFunction> functions;

        public static void Init()
        {
            if (functions != null) return;
            functions = new Dictionary<string, TFunction>();

            StdLibrary.Init();
        }

        public static TFunction GetFunction(string functionName, bool includeStdLibrary = true)
        {
            TFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return includeStdLibrary ? StdLibrary.GetFunction(functionName) : null;
        }

        public static TException AddFunction(Interpreter interpreter, TFunction function)
        {
            TFunction existingFunction = StdLibrary.GetFunction(function.name);
            if (existingFunction != null)
                return new TException(interpreter, "Standard library function with name '" + function.name + "' already exists");

            existingFunction = GetFunction(function.name, false);
            if (existingFunction == null) functions.Add(function.Name, function);
            else
            {
                existingFunction.hardCodedFunction = null;
                existingFunction.customFunction = "";
                existingFunction.block = null;
                existingFunction.CopyFrom(function);
            }

            return null;
        }

        private string name;
        public string Name { get { return name; } }

        private string[] argNames;
        public string[] ArgNames { get { return argNames; } }
        public int ArgCount { get { if (argNames == null) return -1; return argNames.Length; } }

        private TType[] defaultArgs;
        public TType[] DefaultArgs { get { return defaultArgs; } }

        private function hardCodedFunction;
        public function HardCodedFunction { get { return hardCodedFunction; } }

        private string customFunction;
        public string CustomFunction { get { return customFunction; } }

        private TBlock block;
        public TBlock Block { get { return block; } }

        public TFunction(string name, function function, string[] argNames, TType[] defaultArgs)
        {
            this.name = name;
            hardCodedFunction = function;
            customFunction = "";
            block = null;

            if (argNames == null)
            {
                this.argNames = null;
                this.defaultArgs = null;
            }
            else
            {
                this.argNames = new string[argNames.Length];
                if (argNames.Length > 0) argNames.CopyTo(this.argNames, 0);

                if (defaultArgs == null) this.defaultArgs = new TType[argNames.Length];
                else
                {
                    this.defaultArgs = new TType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(this.defaultArgs, 0);
                }
            }
        }

        public TFunction(string name, string function, string[] argNames, TType[] defaultArgs)
        {
            this.name = name;
            hardCodedFunction = null;
            customFunction = function;
            block = null;

            if (argNames == null)
            {
                this.argNames = null;
                this.defaultArgs = null;
            }
            else
            {
                this.argNames = new string[argNames.Length];
                if (argNames.Length > 0) argNames.CopyTo(this.argNames, 0);

                if (defaultArgs == null) this.defaultArgs = new TType[argNames.Length];
                else
                {
                    this.defaultArgs = new TType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(this.defaultArgs, 0);
                }
            }
        }

        public TFunction(string name, TBlock block, string[] argNames, TType[] defaultArgs)
        {
            this.name = name;
            this.block = block;
            hardCodedFunction = null;
            customFunction = "";

            if (argNames == null)
            {
                this.argNames = null;
                this.defaultArgs = null;
            }
            else
            {
                this.argNames = new string[argNames.Length];
                if (argNames.Length > 0) argNames.CopyTo(this.argNames, 0);

                if (defaultArgs == null) this.defaultArgs = new TType[argNames.Length];
                else
                {
                    this.defaultArgs = new TType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(this.defaultArgs, 0);
                }
            }
        }

        public TType Call(Interpreter interpreter, TType value)
        {
            TException exception = new TException(interpreter, "Incorrect number of arguments for function '" + name + "'");

            TArgumentList argList = value as TArgumentList;
            if (argList == null)
            {
                argList = new TArgumentList();
                argList.Add(value);
            }

            if (argNames != null)
            {
                if (argList.Count < argNames.Length)
                {
                    for (int i = argList.Count; i < defaultArgs.Length; ++i)
                    {
                        if (defaultArgs[i] == null) break;
                        else argList.Add(defaultArgs[i]);
                    }
                }
                if (argList.Count != argNames.Length)
                {
                    exception.Hint = argList.Count.ToString() + " out of " + argNames.Length.ToString() + " given";
                    return exception;
                }
            }

            interpreter.Stack.Push();
            int stackLevel = interpreter.Stack.Level;

            if (argNames == null)
            {
                for (int i = 0; i < argList.Count; ++i) interpreter.Stack.AddVariable(new TVariable("arg" + i.ToString(), argList[i]));
            }
            else
            {
                for (int i = 0; i < argList.Count; ++i) interpreter.Stack.AddVariable(new TVariable(argNames[i], argList[i]));
            }


            TType returnValue = null;
            bool dontPop = false;

            if (hardCodedFunction != null) returnValue = hardCodedFunction.Invoke(interpreter, argList);
            else if (customFunction != "")
            {
                returnValue = interpreter.Interpret(customFunction, true);
                if (interpreter.Stack.Level < stackLevel) dontPop = true;
            }
            else if (block != null)
            {
                TBlock previousCurrentBlock = interpreter.CurrentBlock;
                interpreter.CurrentBlock = block;
                block.ResetLine();

                while (!block.EndOfBlock)
                {
                    returnValue = block.ExecuteLine(interpreter);

                    if (interpreter.Stack.Level < stackLevel)
                    {
                        dontPop = true;
                        break;
                    }
                    else if (returnValue is TException) break;
                }

                interpreter.CurrentBlock = previousCurrentBlock;
            }

            {
                TVariable variable = returnValue as TVariable;
                if (variable != null) returnValue = variable.Value;
            }

            if (!dontPop) interpreter.Stack.Pop();

            return returnValue ?? TNil.Instance;
        }

        public void CopyFrom(TFunction otherFunction)
        {
            name = otherFunction.name;
            hardCodedFunction = otherFunction.hardCodedFunction;
            customFunction = otherFunction.customFunction;
            block = otherFunction.block;

            if (otherFunction.argNames == null)
            {
                argNames = null;
                defaultArgs = null;
            }
            else
            {
                argNames = new string[otherFunction.argNames.Length];
                if (argNames.Length > 0) otherFunction.argNames.CopyTo(argNames, 0);

                if (otherFunction.defaultArgs == null) defaultArgs = new TType[otherFunction.argNames.Length];
                else
                {
                    defaultArgs = new TType[otherFunction.argNames.Length];
                    if (defaultArgs.Length > 0) otherFunction.defaultArgs.CopyTo(defaultArgs, 0);
                }
            }
        }

        public override string TypeName { get { return "function"; } }

        public override string ToCSString()
        {
            return name;
        }
    }
}
