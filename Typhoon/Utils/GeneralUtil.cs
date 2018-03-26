using System;

namespace Typhoon.Utils
{
    internal static class GeneralUtil
    {
        public static void Usage(String msg)
        {
            const String usage = @"
    Usage: 

    Typhoon -mode=<console|pyrepl|csrepl|exec> 
                   console: No options
                   csrepl: 
                          -type=<multi|...>
                   pyrepl:
                          -type=<single := one line|multi := multi line>
                   exec:   
                          -type=[py := python dlr|cs := c# extension contract source ] 
                            py:
                              -method=<sfile := source file>
                            cs:
                              -method=<sfile := source file |afile := assembly file>

                          -resource=<resource_location := path to file>
                ";
            const String examples = @"

    Examples:

        // Console:
    Typhoon.exe  -mode=console

        // Python DLR REPL (multiline)
    Typhoon.exe  -mode=csrepl -type=multi

        // Python DLR REPL (one-liners)
    Typhoon.exe  -mode=pyrepl -type=single

        // Python DLR Execute script
    Typhoon.exe  -mode=exec -type=py -method=sfile -resource=.\test.py

        // CSharp Code execution: Extension contract dynamic compilation and execution
    Typhoon.exe  -mode=exec -type=cs -method=xfile -resource=Extensions\WmiQuery.cs -class=Typhoon.Extensions.ClipboardManager

        // CSharp Code Extension 1) compile into Assembly, 2) Load and execute
    Typhoon.exe  -mode=comp -type=cs -resource=..\..\Examples\Extensions\ClipboardManager.cs
    Typhoon.exe  -mode=exec -type=cs -method=afile -resource=.\tmp4E52.dll -class=Typhoon.Extensions.ClipboardManager

                ";
            Console.WriteLine(usage);
            Console.WriteLine();
            Console.WriteLine(examples);
            Console.WriteLine();
            Console.WriteLine("ERROR: {0}", msg);
            Environment.Exit(1);
        }
    }
}
