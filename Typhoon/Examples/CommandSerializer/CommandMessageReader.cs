using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon
{
    public static class CommandMessageReader
    {

        public static void MsgRead()
        {
            // Now we can read the serialized book ...  
            System.Xml.Serialization.XmlSerializer reader =
                new System.Xml.Serialization.XmlSerializer(typeof(CommandMessage));
            System.IO.StreamReader file = new System.IO.StreamReader(
                @"c:\temp\SerializationOverview.xml");
            CommandMessage overview = (CommandMessage)reader.Deserialize(file);
            file.Close();

            Console.WriteLine(overview.title);

        }
    }
}
