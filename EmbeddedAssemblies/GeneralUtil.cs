using System;

namespace Cradle
{
    internal static class GeneralUtil
    {
        public static void Usage(String msg)
        {
            const String usage = @"
    Usage: 

    Typhoon -mode=<console|pyrepl|csrepl|exec> 
            console: No options
            csrepl : No options
            pyrepl :
                   -type=<single|multi>
            exec   -type=[py|cs] 
                   -method=<mem|file|net>
                   -resource=<resource_loc>
                ";
            const String examples = @"

    Examples:

    Typhoon.exe  -mode=console
    Typhoon.exe  -mode=csrepl
    Typhoon.exe  -mode=pyrepl -type=single
    Typhoon.exe  -mode=exec -type=py -method=file -resource=.\test.py

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
