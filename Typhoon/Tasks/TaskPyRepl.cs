using System;

namespace Typhoon
{
    class TaskPyRepl
    {
        private String type;

        private dynamic pengine;
        private dynamic pscope;
        private dynamic pythonScript;

        public TaskPyRepl(String arg)
        {
            type = arg;
            pengine = IPythonUtil.GetPyEngine();
            pscope = IPythonUtil.GetNewScope();

            this.ProcessCommand();
        }

        private void ProcessCommand()
        {

            switch (type)
            {
                case "single":
                    Console.WriteLine(Commander.TPyHelp);
                    replSingle();
                    break;
                case "multi":
                    Console.WriteLine(Commander.TPyHelp);
                    replMulti();
                    break;
                default:
                    throw new ApplicationException($"Invalid mode. {Commander.TPyHelp}");
            }

        }

        private void replMulti()
        {

            //String result = Console.ReadLine().Trim();

            bool replRunning = true;

            string replPrompt = ">>> ";
            string replResult = "#import traceback; ";
            while (replRunning)
            {

                Console.Write("{0}", replPrompt);
                string replLine = Console.ReadLine();
                
                // Imeddiately exit REPL
                if (replLine.Equals("QUIT"))
                {
                    replRunning = false;
                    continue;
                }
                // 
                if (replLine.Equals("END"))
                {
                    Console.WriteLine("End input. Executing: {0}", replResult);

                    this.pythonScript = this.pengine.CreateScriptSourceFromString(replResult);
                    //errors = ErrorListener();
                    //pengine.Compile(pythonScript);

                    try
                    {
                        Console.WriteLine(pythonScript);
                        pythonScript.Execute();
                    }
                    catch (Exception mse)
                    {
                        Console.Error.WriteLine("Code issues {0} {1}", mse.Message, mse.StackTrace);
                    }

                    replPrompt = ">>> ";
                    replResult = "";
                }
                else
                {
                    replResult += Environment.NewLine;
                    replResult += replLine;
                    replPrompt = "..> ";
                }
            }

        }
        private void replSingle()
        {
            bool replRunning = true;

            string replPrompt = ">>> ";
            while (replRunning)
            {
                Console.Write("{0}", replPrompt);
                string replLine = Console.ReadLine();
                // Imeddiately exit REPL
                if (replLine.Equals("QUIT"))
                {
                    replRunning = false;
                    continue; 
                }

                try
                {

                        this.pengine.Execute(replLine, this.pscope);

                }
                catch (Exception mse)
                {
                    Console.Error.WriteLine("Code issues {0} {1}", mse.Message, mse.StackTrace);
                }
            }
        }


    }

}
