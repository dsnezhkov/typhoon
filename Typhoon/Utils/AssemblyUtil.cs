using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Typhoon
{
    static class AssemblyUtil
    {
        private static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private static string[] EmbeddedLibraries =
            ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();


        /// <summary>
        /// Attach custom event handler to resolve Assemblies from embedded resources
        /// </summary>
        internal static void SetAssemblyResolution()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyUtil.CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AssemblyUtil.CurrentDomain_EHandler);

        }

        private static void CurrentDomain_EHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("Handler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }


        /// <summary>
        /// Load Initial assemblies
        /// </summary>
        /// <param name="assemblynames"></param>
        /// <param name="assemblyDict"></param>
        internal static void LoadInitialCoreAssemblies(
                String[] assemblynames, ref Dictionary<string, Assembly> assemblyDict )
        {

            foreach (String assemblyname in assemblynames)
            {
                Assembly assembly = Assembly.Load(assemblyname);
                assemblyDict.Add(assemblyname, assembly);
            }

        }

        /// <summary>
        /// List assemblies in passed AppDomain
        /// </summary>
        /// <param name="ad"></param>
        internal static void showAssembliesInDomain(AppDomain ad)
        {

            Assembly[] assems = ad.GetAssemblies();

            Console.WriteLine("List of assemblies loaded in current appdomain:");
            foreach (Assembly assem in assems)
                Console.WriteLine(assem.ToString());
        }

        /// <summary>
        /// Custom Resolve assembly load (embedded resources)
        /// 1. Get assembly name
        /// 2. Get resource name
        /// 3. Load assembly from resource
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";

            if (ConfigUtil.DEBUG)
            {
                Console.WriteLine("Loading {0} ... ", assemblyName);
            }

            var resourceName = EmbeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
            if (resourceName == null)
            {
                return null;
            }

            using (var stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
            {

                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                return Assembly.Load(bytes);
            }
        }
    }
}
