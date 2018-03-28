using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Typhoon.MStore
{
    static class MServer
    {
        static string NAME = "MStore"; //TODO: Tune
        static ManualResetEvent evt = new ManualResetEvent(false);

        static private bool ProcessCommandS(String dcommand)
        {
            try
            {
                CommandMessage dscommand = CommandSerializers.Deserialize<CommandMessage>(dcommand);
                Console.WriteLine("S:Deserialized: {0}", dscommand);
            }catch(Exception e)
            {
                Console.WriteLine("S:Deserialization Error: {0}", e.Message );
                return false;
            }
            return true;
        }

        static void Server()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream(NAME, 
                                PipeDirection.InOut,
                                1,
                                PipeTransmissionMode.Message
                            ))
            {
                evt.Set();
                Console.WriteLine("Waiting for connection ...");
                server.WaitForConnection();
                Console.WriteLine("Connection established.");

                using (StreamWriter sw = new StreamWriter(server))
                {
                    while (true)
                    {

                        MemoryStream ms = new MemoryStream();
                        try
                        {
                            byte[] buffer = new byte[0x1000];
                            do {
                                ms.Write(buffer, 0, server.Read(buffer, 0, buffer.Length));
                            }while (!server.IsMessageComplete);

                        }catch (IOException sio){
                            Console.WriteLine("Receive: IO exception {0}", sio.Message);
                        }

                        string stringData = Encoding.UTF8.GetString(ms.ToArray());
                        CommandSerializers.WriteYellow("Server received REQUEST: Serialized as: {0}\n", stringData);

                        CommandMessage sm = new CommandMessage(); 
                        if ( ProcessCommandS(stringData)) {
                            sm.meta = "A";
                        }else{
                            sm.meta = "Q";
                        }

                        String sms = CommandSerializers.Serialize(sm);
                        CommandSerializers.WriteYellow("Server sends RESPONSE: Serialized as: {0}\n", sms);

                        try
                        {
                            sw.WriteLine(sms);
                            sw.Flush();
                        }catch (IOException sio){
                            Console.WriteLine("Receive: IO exception {0}", sio.Message);
                        }

                        Thread.Sleep(1000); // TODO: tune
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            new Thread(Server).Start();
            evt.WaitOne();
        }
    }
}
