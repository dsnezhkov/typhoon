 1 using System;
 2 using System.IO;
 3 using System.IO.Pipes;
 4 using System.Threading;
 5 
 6 class Program
 7 {
 8     static string NAME = "Demo";
 9     static ManualResetEvent evt = new ManualResetEvent(false);
10 
11     static void Server()
12     {
13         using (NamedPipeServerStream server = new NamedPipeServerStream(NAME, PipeDirection.InOut))
14         {
15             evt.Set();
16             WriteGreen("Waiting for connection ...");
17             server.WaitForConnection();
18             WriteGreen("Connection established.");
19 
20             using (StreamReader sr = new StreamReader(server))
21             {
22                 using (StreamWriter sw = new StreamWriter(server))
23                 {
24                     int i = 0;
25                     while (true)
26                     {
27                         i = int.Parse(sr.ReadLine());
28                         string s = Convert.ToString(i, 2);
29                         WriteGreen("SERVER: Received {0}, sent {1}.", i, s);
30                         sw.WriteLine(s);
31                         sw.Flush();
32                         Thread.Sleep(1000);
33                     }
34                 }
35             }
36         }
37     }
38 
39     static void Main(string[] args)
40     {
41         new Thread(Server).Start();
42         evt.WaitOne();
43 
44         using (NamedPipeClientStream client = new NamedPipeClientStream(".", NAME, PipeDirection.InOut))
45         {
46             WriteRed("Connecting to server ...");
47             client.Connect();
48             WriteRed("Connected.");
49 
50             using (StreamWriter sw = new StreamWriter(client))
51             {
52                 using (StreamReader sr = new StreamReader(client))
53                 {
54                     Random rand = new Random();
55 
56                     while (true)
57                     {
58                         int i = rand.Next(1000);
59                         sw.WriteLine(i);
60                         sw.Flush();
61                         string s = sr.ReadLine();
62                         WriteRed("CLIENT:  Sent {0}, received {1}.", i, s);
63                     }
64                 }
65             }
66         }
67     }
68 
69     static void WriteRed(string msg, params object[] p)
70     {
71         Console.ForegroundColor = ConsoleColor.Red;
72         Console.WriteLine(msg, p);
73         Console.ResetColor();
74     }
75 
76     static void WriteGreen(string msg, params object[] p)
77     {
78         Console.ForegroundColor = ConsoleColor.Green;
79         Console.WriteLine(msg, p);
80         Console.ResetColor();
81     }
82 }





using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
 
namespace AnonymousPipesIntraProcess
{
 class Program
 {
  
 static void Main(string[] args)
 {
 using (AnonymousPipeServerStream pipedServer = new AnonymousPipeServerStream(PipeDirection.Out))
 {
 
 Thread child = new Thread(new ParameterizedThreadStart(childThread));
 child.Start(pipedServer.GetClientHandleAsString());
  
 using (StreamWriter sw = new StreamWriter(pipedServer))
 {
 var data = string.Empty;
 sw.AutoFlush = true;
 while (!data.Equals("quit", StringComparison.InvariantCultureIgnoreCase))
 {
 pipedServer.WaitForPipeDrain();
 Console.WriteLine("SERVER : ");
 data = Console.ReadLine();
 sw.WriteLine(data);
 }
 
 
 }
 
 }
 }
 
 public static void childThread(object parentHandle)
 {
 using (AnonymousPipeClientStream pipedClient = new AnonymousPipeClientStream(PipeDirection.In, parentHandle.ToString()))
 {
 using (StreamReader reader = new StreamReader(pipedClient))
 {
 var data = string.Empty;
 while ((data = reader.ReadLine()) != null)
 {
 Console.WriteLine("CLIENT:" + data.ToString());
 }
 Console.Write("[CLIENT] Press Enter to continue...");
 Console.ReadLine();
 }
 }
 }
 
  
 }
}