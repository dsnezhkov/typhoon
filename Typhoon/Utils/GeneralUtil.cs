using System;

namespace Typhoon.Utils
{
    internal static class GeneralUtil
    {
        public static void Usage(String msg)
        {
            const String banner = @"

==========================================
  _____         ___ _                  
 |_   _|  _    | _ \ |_  ___  ___ _ _  
   | || || |   |  _/ ' \/ _ \/ _ \ ' \ 
   |_| \_, |   |_| |_||_\___/\___/_||_|
       |__/                          

    Managed Execution Toolkit.
      Features CSaw, DLRium
==========================================

";
            const String usage = @"
USAGE: 

    Typhoon.exe -mode=<console|pyrepl|csrepl|exec|comp> <mode options>

     console: Interactive.

     csrepl: 
         Mode options:
            -type=<multi|...>

     pyrepl:
         Mode options:
            -type=<single := one line|multi := multi line>

     exec:   
         Mode options:
            -type=<py := python dlr|cs := c# extension contract source>  <type options>

                Type options:
                py:
                    -method=<sfile := source file>
                    [-targs=<targs := arguments to .py script>]
                cs:
                    -method=<sfile := source file |afile := assembly file>
                    -class=<class := class.RunCode()>

         Resource options:
            -resource=<resource_location := path to file>

     comp: 
            -type=<cs := c# extension contract source>  <type options>

                Type options:
                -resource=<resource_location := path to file>
    ";
            const String examples = @"
EXAMPLES:

    Console: Interactive mode. Alternate features
        Typhoon.exe  -mode=console

    === CSaw ===
        CS REPL
            Typhoon.exe  -mode=csrepl -type=multi

        CS Extension contract code compilation and execution:
            Typhoon.exe  -mode=exec -type=cs -method=sfile -resource=..\..\Examples\\Extensions\Scratch.cs -class=Typhoon.Extensions.Scratch

        CS code extension: Phased compilation and execution
            Compile CS code into Assembly DLL:
                Typhoon.exe  -mode=comp -type=cs -resource=..\..\Examples\Extensions\ClipboardManager.cs

            Load Assembly DLL and execute an extension contract:
                Typhoon.exe  -mode=exec -type=cs -method=afile -resource=.\tmp4E52.dll -class=Typhoon.Extensions.ClipboardManager

    === DLRium ===
        Python DLR REPL
            Typhoon.exe  -mode=pyrepl -type=single
            Typhoon.exe  -mode=pyrepl -type=multi

        Python DLR Execute script
            Typhoon.exe  -mode=exec -type=py -method=sfile -resource=.\test.py

        Python DLR Execute script (with args passed to script)
            Typhoon.exe  -mode=exec -type=py -method=sfile -resource=..\..\Examples\py2exe\Pyc.py 
                            -targs="" /target:exe /platform:x64 /main:test_main.py test_main.py""

        Python DLR compile to EXE via DLR
           Typhoon.exe  -mode=exec -type=py -method=sfile -resource=..\..\Examples\py2exe\Pyc.py 
                            -targs="" /target:exe /platform:x64 /main:test_main.py test_main.py""

        Python DLR compile to DLL via DLR
           Typhoon.exe  -mode=exec -type=py -method=sfile -resource=..\..\Examples\py2exe\Pyc.py 
                            -targs="" /target:dll /platform:x86 ""

    ";

            LookAndFeel.SetColorConsole(ConsoleColor.DarkGreen, null);
            Console.WriteLine(banner);
            Console.WriteLine();
            Console.WriteLine(usage);
            Console.WriteLine();
            Console.WriteLine(examples);
            Console.WriteLine();
            LookAndFeel.SetColorConsole(ConsoleColor.DarkRed, null);
            Console.WriteLine("ERROR: {0}", msg);
            LookAndFeel.ResetColorConsole(true);
            Environment.Exit(1);
        }
    }
}
