using System;

namespace Typhoon
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
                          -type=<single|multi>
                   exec:   
                          -type=[py|cs] 
                            py:
                              -method=<sfile>
                            cs:
                              -method=<sfile|xfile|lfile>

                          -resource=<resource_location>
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

    // CSharp Code execution: Extension contract
    Typhoon.exe  -mode=exec -type=cs -method=xfile -resource=Extensions\WmiQuery.cs -class=Typhoon.Extensions.ClipboardManager

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
