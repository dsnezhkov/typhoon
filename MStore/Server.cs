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

                    while (true)
                    {

                        MemoryStream ms = new MemoryStream();
                        try
                        {
                            byte[] buffer = new byte[1024];
                            var sb = new StringBuilder();
                            int read;

                            while ((read = server.Read(buffer, 0, buffer.Length)) > 0 && 
                                        !server.IsMessageComplete) {
                                CommandSerializers.WriteYellow("Server received length {0}\n", read);
                                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                            }

                            using (var writer = new StreamWriter(ms))
                            {
                                writer.Write(sb);
                            }
                        } catch (IOException sio){
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

                        sm.data = "Some data";
                        sm.data = null;
                        String sms = CommandSerializers.Serialize(sm);

                        var smsBytes = Encoding.ASCII.GetBytes(sms);
                        CommandSerializers.WriteYellow("Server sends RESPONSE: Serialized as: {0}\n", sms);

                        try
                        {
                            server.Write(smsBytes, 0, smsBytes.Length);
                            server.Flush();
                        }catch (IOException sio){
                            Console.WriteLine("Receive: IO exception {0}", sio.Message);
                        }

                        Thread.Sleep(1000); // TODO: tune
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
