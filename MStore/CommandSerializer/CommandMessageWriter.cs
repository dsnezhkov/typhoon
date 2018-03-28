using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon.MStore
{
    public static class CommandMessageWriter
    {

        public static void MsgWrite(CommandMessage)
        {
            // First write something so that there is something to read ...  
            var b = new CommandMessage { title = "Serialization Overview" };
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(CommandMessage));
            var wfile = new System.IO.StreamWriter(@"c:\temp\SerializationOverview.xml");
            writer.Serialize(wfile, b);
            wfile.Close();
        }
    }



}
