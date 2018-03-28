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

        public override string ToString()
        {
            return String.Format("Meta: {0}, Data: {1}", meta, data);
        }
    } 
}
