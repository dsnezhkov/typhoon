using System;

namespace Typhoon
{
    
    static class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            // Load Assemblies or setup loading paths
            AssemblyUtil.SetAssemblyResolution();

            var parameters = new ParametersParser();
            if ( !parameters.HasKeyAndValue("-mode") )
            {
                GeneralUtil.Usage("Option  [-mode=<value>]  is required");

            }else{
                String mode = parameters.GetFirstValue("-mode");
                switch ( mode.ToLower() )
                {
                    case "console" :
                    case "con" :
                    case "c" :
                        OptionStarter.ModeConsole();
                        break;
                    case "exec":
                        if (parameters.HasKeyAndValue("-type") 
                            && parameters.HasKeyAndValue("-resource") 
                            && parameters.HasKeyAndValue("-method") 
                            && parameters.GetFirstValue("-type") != String.Empty
                            && parameters.GetFirstValue("-resource") != String.Empty
                            && parameters.GetFirstValue("-method") != String.Empty)
                        {
                            String type = parameters.GetFirstValue("-type");
                            String resource = parameters.GetFirstValue("-resource");
                            String method = parameters.GetFirstValue("-method");
                            String klass = String.Empty;

                            switch (type.ToLower())
                            {
                                case "python" :
                                case "py" :
                                    Console.WriteLine("Python DLR");
                                    OptionStarter.ModePyExec(resource, method);
                                    break;
                                case "csharp" :
                                case "cs" :
                                    if (parameters.HasKeyAndValue("-class")){
                                        klass = parameters.GetFirstValue("-class");
                                    }
                                    Console.WriteLine("Dynamic CS");
                                    OptionStarter.ModeCSExec(resource, method, klass);
                                    break;
                                default:
                                    GeneralUtil.Usage("Unknown type of EXEC: " + type);
                                    break;
                            }
                        }else
                        {
                            GeneralUtil.Usage("Specify proper -type, -resource, -method to get to the resource");
                        }
                        break;
                    case "pyrepl":
                        if (parameters.HasKeyAndValue("-type"))
                        {
                            String type = parameters.GetFirstValue("-type");
                            switch ( type.ToLower())
                            {
                                case "single" :
                                case "s" :
                                    OptionStarter.ModePyRepl("single");
                                    break;
                                case "multi" :
                                case "m" :
                                    OptionStarter.ModePyRepl("multi");
                                    break;
                                default:
                                    GeneralUtil.Usage("Unknown type of PyREPL " + type);
                                    break;
                            }
                        }else
                        {
                            GeneralUtil.Usage("Specify type of PyREPL ");
                        }
                        break;
                    case "csrepl" :
                        if (parameters.HasKeyAndValue("-type"))
                        {
                            String type = parameters.GetFirstValue("-type");
                            switch (type.ToLower())
                            {
                                case "multi":
                                case "m":
                                    OptionStarter.ModeCSRepl("multi");
                                    break;
                                default:
                                    GeneralUtil.Usage("Unknown type of CSREPL " + type);
                                    break;
                            }
                        }
                        else
                        {
                            GeneralUtil.Usage("Specify type of CsREPL ");
                        }
                        break;
                    default:
                        GeneralUtil.Usage("Unknown mode " + mode);
                        break;
                }
            }
        }
    }
}
