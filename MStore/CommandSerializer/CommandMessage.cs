using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon.MStore
{
    [Serializable]
    public class CommandMessage
    {
        public String meta;
        public String data;
        public Byte[] blob;

        public override string ToString()
        {
            return String.Format("Meta: {0}, Data: {1}, Blob: len {0}", meta, data, blob?.Length);
        }
    } 
}
