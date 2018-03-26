using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Typhoon.Utils;

namespace Typhoon
{
    internal static class MMResourceRunner
    {
        /// <summary>
        /// Run Cs extension ora Py Script
        /// </summary>
        /// <param name="mmLocation"></param>
        public static void RunMMRecord(String mmLocation)
        {
            Dictionary<String, MMRecord>  mmfRepo = MMemoryLoader.GetMMFRepo();
            Console.WriteLine("In RunMMRecord");

            if (mmfRepo.ContainsKey(mmLocation))
            {
                MMRecord mmr;
                if (mmfRepo.TryGetValue(mmLocation, out mmr))
                {
                    Console.WriteLine("After TryGetValue");
                    Console.WriteLine("Found {0}, {1}, {2}", mmr.ResourceSize, mmr.ResourceType, mmr.ResourceMMFile );

                    switch (mmr.ResourceType.ToLower())
                    {
                        case "py":
                            try
                            {
                                dynamic pengine = IPythonUtil.GetPyEngine();
                                dynamic pscope = IPythonUtil.GetNewScope();
                                String pyCodeContent = MMemoryLoader.MMRecordContentString(mmLocation);
                                if (ConfigUtil.DEBUG)
                                {
                                    Console.WriteLine("Python Code: \n\n__BEGIN__\n\n{0}\n\n__END__\n\n", pyCodeContent);
                                }

                                dynamic pythonScript = IPythonUtil.GetPyEngine().CreateScriptSourceFromString(pyCodeContent);
                                Console.WriteLine("Execute Python script ({0}) contents from memory", mmLocation);
                                pythonScript.Execute(pscope);
                            }
                            catch (Exception ae)
                            {
                                Console.WriteLine("Iron Python Scope/Execution not created: {0}", ae.Message);
                            }
                            break;
                        case "cs":

                            Console.WriteLine("ResourceType {0}", mmr.ResourceType);
                            String csCodeContent = MMemoryLoader.MMRecordContentString(mmLocation);

                            Console.WriteLine("csCodeContent {0}", csCodeContent);

                            // Load CS Source Code of the extension, MMRecord's name is the name of the TypeToRun param
                            // when MMemoryLoader loads resource it might contain extension on the file, strip it when passing to TypeToRun
                            // Name of the MMRecord is the name of the TypeToRun minus extension. Name your extension file accordingly
                            // Example: If Memory File is WmiQuery.cs then TypeToRun is `WmiQuery`
                            DynCSharpRunner.CompileRunXSource(csCodeContent,
                                            String.Join(".", new String[2] {
                                                            "Typhoon.Extensions",
                                                             mmLocation.Replace(
                                                                 String.Concat(new String[] {".", mmr.ResourceType }),
                                                                 String.Empty)}));
                            break;
                        default:
                            Console.WriteLine("Unknown ResourceType {0}", mmr.ResourceType);
                            break;
                    }
                }else
                {
                    Console.WriteLine("Unable to get {0}. Valid value?", mmLocation);
                }
            }else
            {
                Console.WriteLine("Unable to find {0}. Valid value?", mmLocation );

            }
        }

    }
}
