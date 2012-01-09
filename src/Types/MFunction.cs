using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage.Types
{
    class MFunction : MType
    {
        public delegate MType function(Interpreter interpreter, MArgumentList args);
        private static Dictionary<string, MFunction> functions;

        public static void Init()
        {
            if (functions != null) return;
            functions = new Dictionary<string, MFunction>();

            StdLibrary.Init();
        }

        public static MFunction GetFunction(string functionName, bool includeStdLibrary = true)
        {
            MFunction returnValue;
            if (functions.TryGetValue(functionName, out returnValue)) return returnValue;
            return includeStdLibrary ? StdLibrary.GetFunction(functionName) : null;
        }

        public static MException AddFunction(Interpreter interpreter, MFunction function)
        {
            MFunction existingFunction = StdLibrary.GetFunction(function.name);
            if (existingFunction != null)
                return new MException(interpreter, "Standard library function with name '" + function.name + "' already exists");

            existingFunction = GetFunction(function.name, false);
            if (existingFunction == null) functions.Add(function.Name, function);
            else existingFunction.CopyFrom(function);

            return null;
        }

        private string name;
        public string Name { get { return name; } }

        private string[] argNames;
        public string[] ArgNames { get { return argNames; } }
        public int ArgCount { get { if (argNames == null) return -1; return argNames.Length; } }

        private MType[] defaultArgs;
        public MType[] DefaultArgs { get { return defaultArgs; } }

        private function hardCodedFunction;
        public function HardCodedFunction { get { return hardCodedFunction; } }

        private string customFunction;
        public string CustomFunction { get { return customFunction; } }

        public MFunction(string name, function function, string[] argNames, MType[] defaultArgs)
        {
            this.name = name;
            hardCodedFunction = function;
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

                if (defaultArgs == null) this.defaultArgs = new MType[argNames.Length];
                else
                {
                    this.defaultArgs = new MType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(this.defaultArgs, 0);
                }
            }
        }

        public MFunction(string name, string function, string[] argNames)
        {
            this.name = name;
            hardCodedFunction = null;
            customFunction = function;
            if (argNames == null)
            {
                this.argNames = null;
                this.defaultArgs = null;
            }
            else
            {
                this.argNames = new string[argNames.Length];
                if (argNames.Length > 0) argNames.CopyTo(this.argNames, 0);

                if (defaultArgs == null) this.defaultArgs = new MType[argNames.Length];
                else
                {
                    this.defaultArgs = new MType[argNames.Length];
                    if (defaultArgs.Length > 0) defaultArgs.CopyTo(this.defaultArgs, 0);
                }
            }
        }

        public MType Call(Interpreter interpreter, MType value)
        {
            MException exception = new MException(interpreter, "Incorrect number of arguments for function '" + name + "'");

            MArgumentList argList = value as MArgumentList;
            if (argList == null)
            {
                argList = new MArgumentList();
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
            if (argNames == null)
            {
                for (int i = 0; i < argList.Count; ++i) interpreter.Stack.AddVariable(new MVariable("arg" + i.ToString(), argList[i]));
            }
            else
            {
                for (int i = 0; i < argList.Count; ++i) interpreter.Stack.AddVariable(new MVariable(argNames[i], argList[i]));
            }


            MType returnValue;
            if (hardCodedFunction == null) returnValue = interpreter.Interpret(customFunction, true);
            else returnValue = hardCodedFunction.Invoke(interpreter, argList);

            MVariable variable = returnValue as MVariable;
            if (variable != null) returnValue = variable.Value;

            interpreter.Stack.Pop();

            return returnValue;
        }

        public void CopyFrom(MFunction otherFunction)
        {
            name = otherFunction.name;
            hardCodedFunction = otherFunction.hardCodedFunction;
            customFunction = otherFunction.customFunction;
            if (otherFunction.argNames == null)
            {
                argNames = null;
                defaultArgs = null;
            }
            else
            {
                argNames = new string[otherFunction.argNames.Length];
                if (argNames.Length > 0) otherFunction.argNames.CopyTo(argNames, 0);

                if (otherFunction.defaultArgs == null) defaultArgs = new MType[otherFunction.argNames.Length];
                else
                {
                    defaultArgs = new MType[otherFunction.argNames.Length];
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
