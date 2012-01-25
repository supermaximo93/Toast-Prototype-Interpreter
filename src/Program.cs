using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MathsLanguage
{
    class Program
    {
        static void Main(string[] args)
        {
            Types.MFunction.Init();
            Interpreter interpreter = new Interpreter();
            interpreter.Run(args.Length > 0 ? args[0] : "");
        }
    }
}
