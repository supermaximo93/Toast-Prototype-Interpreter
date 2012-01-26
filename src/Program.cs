using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toast
{
    class Program
    {
        static void Main(string[] args)
        {
            Types.TFunction.Init();
            Interpreter interpreter = new Interpreter();
            interpreter.Run(args.Length > 0 ? args[0] : "");
        }
    }
}
