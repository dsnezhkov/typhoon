using System;
using System.IO;
using System.Xml.Serialization;

namespace Typhoon.MStore
{
    public static class CommandSerializers
    {
        public static T Deserialize<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StringReader textReader = new StringReader(toDeserialize);
            return (T)xmlSerializer.Deserialize(textReader);
        }

        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StringWriter textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }

        public static void WriteYellow(string msg, params object[] p)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg, p);
            Console.ResetColor();
        }
        public static void WriteGreen(string msg, params object[] p)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg, p);
            Console.ResetColor();
        }
    }
}
