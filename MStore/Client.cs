using System;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Typhoon.MStore
{
    static class MClient
    {
        static string NAME = "MStore";
        static ManualResetEvent evt = new ManualResetEvent(false);

        static private bool ProcessCommandC(String dcommand)
        {
            try
            {
                CommandMessage dscommand = CommandSerializers.Deserialize<CommandMessage>(dcommand);
                Console.WriteLine("C:Deserialized: {0}", dscommand);
            }catch(Exception e)
            {
                Console.WriteLine("C:Deserialization Error: {0}", e.Message );
                return false;
            }
            return true;
        }

        static void Main(string[] args)
        {
            new Thread(Client).Start();
            evt.WaitOne();
        }
        static void Client()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", NAME, 
                                PipeDirection.InOut
                            ))
            {
                Console.WriteLine("Connecting to server ...");
                client.Connect();
                client.ReadMode = PipeTransmissionMode.Message;
                Console.WriteLine("Connected.");

                while (true)
                {

                    Byte[] randbytes = File.ReadAllBytes(@".\MStore.pdb");

                    CommandMessage cm = new CommandMessage { meta = "Q", data = "Data", blob = randbytes };
                    String cms = CommandSerializers.Serialize(cm);
                    var cmsBytes = Encoding.ASCII.GetBytes(cms);
                    CommandSerializers.WriteGreen(
                        "Client sends REQUEST of ({0}: Serialized as: {1}\n", cmsBytes.Length, cms);
                    try
                    {
                        client.Write(cmsBytes, 0, cms.Length);
                        client.Flush();
                    }catch(IOException sio)
                    {
                        Console.WriteLine("Send: IO exception {0}", sio.Message);
                    }

                    try
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] buffer = new byte[1024];
                        do
                        {
                            ms.Write(buffer, 0, client.Read(buffer, 0, buffer.Length));
                        } while (!client.IsMessageComplete);

                        string stringData = Encoding.UTF8.GetString(ms.ToArray());
                        CommandSerializers.WriteGreen("Client received RESPONSE: De-Serialized as: {0}\n", stringData);
                        ProcessCommandC(stringData);

                    }catch(IOException sio)
                    {
                        Console.WriteLine("Receive: IO exception {0}", sio.Message);
                    }
                }
            }
        }
    }
}
