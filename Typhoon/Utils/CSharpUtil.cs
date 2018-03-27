using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Typhoon.Utils;

namespace Typhoon
{

    internal static class CSharpUtil
    {
        internal static void LoadCSCompilNamespace()
        {

            var assemblyList = new List<String>();


            // Load assemblies
            String[] staticAllLoads= {  "Microsoft.Dynamic", "Microsoft.Scripting", "Microsoft.CSharp"};
            String[] dynamicAllLoads= {  "DynamicLoad", };

            assemblyList.AddRange(staticAllLoads);

            // Only load those that can be found
            foreach (String dllFile in dynamicAllLoads)
            {
                if (File.Exists(String.Join(".", dllFile,"dll")))
                {
                    assemblyList.Add(dllFile);
                }
            }

            String[] assemblyNames =  assemblyList.ToArray();

            Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();
            AssemblyUtil.LoadInitialCoreAssemblies(assemblyNames, ref LoadedAssemblies);

            if (!ConfigUtil.DEBUG)
            {
                AssemblyUtil.showAssembliesInDomain(AppDomain.CurrentDomain);
            }

        }

    }


}
