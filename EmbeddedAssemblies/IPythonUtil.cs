using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cradle
{

    internal static class IPythonUtil
    {
        private static dynamic pengine = null;

        /// <summary>
        /// Get Python Engine instance. If it does not exist - Load it (create it)
        /// </summary>
        /// <returns>pengine</returns>
        internal static dynamic GetPyEngine()
        {
            if (pengine == null)
            {
                pengine = LoadIPythonEngine();
            }

            return pengine;
        }

        /// <summary>
        /// Load Python Engine. Initialize it from the assembly. Setup all needed frames
        /// </summary>
        /// <returns>pengine</returns>
        internal static dynamic LoadIPythonEngine()
        {

            if (pengine != null)
            {
                return pengine;
            }

            // Load assemblies
            String[] assemblynames = new String[5] {
                "Microsoft.Dynamic", "Microsoft.Scripting",  "IronPython", "IronPython.Modules", "StdLib"
            };
            Dictionary<string, Assembly> LoadedAssemblies = new Dictionary<string, Assembly>();
            AssemblyUtil.LoadInitialCoreAssemblies(assemblynames, ref LoadedAssemblies);

            if (ConfigUtil.DEBUG)
            {
                AssemblyUtil.showAssembliesInDomain(AppDomain.CurrentDomain);
            }

            // Reflectively load IronPython
            Type Ipytype = LoadedAssemblies["IronPython"].GetType("IronPython.Hosting.Python");

            // Set Python ScriptEngine options
            Dictionary<string, object> options = new Dictionary<string, object>();
            options["Frames"] = true;
            options["FullFrames"] = true;


            // Search for appropriate method for CreateEngine with options. Signature has to match invocation below
            MethodInfo create = Ipytype.GetMethod("CreateEngine", new Type[] { typeof(Dictionary<string, object>) });

            // Invoke static Method with options
            pengine = create.Invoke(null, new object[] { options });

            if (pengine == null)
            {
                throw new ApplicationException("PythonEngine Could not be Loaded");
            }else
            {
                return pengine;
            }

        }
        /// <summary>
        /// Create new execution scope based on the existing Pengine.
        /// </summary>
        /// <returns>pscope</returns>
        internal static dynamic GetNewScope()
        {
            // Engine scope
            dynamic pengine;
            dynamic scope = null;

            try
            {
                pengine = GetPyEngine();
                scope = pengine.CreateScope();

            }catch(Exception e)
            {
                Console.WriteLine("Create Scope fault: {0} {1}", e.Message, e.StackTrace);
            }

            if (scope == null)
            {
                throw new ApplicationException("PythonScope Could not be Created");
            }
            else
            {
                return scope;
            }

        }
    }
}
