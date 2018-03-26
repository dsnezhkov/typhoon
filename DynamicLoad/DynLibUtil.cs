using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicLoad
{
    public static class DynLibUtil
    {

        public static dynamic LoadWinapiPInvoke(string library)
        {
            dynamic dynlibrary = new DynamicDllImport(library, callingConvention: CallingConvention.Winapi);
            return dynlibrary;
        }
    }
}
