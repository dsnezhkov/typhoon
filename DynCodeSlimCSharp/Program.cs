
//using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace DynCodeSlimSharp
{
    public class Stage : MarshalByRefObject
    {
        public Assembly LoadAssembly(Byte[] data)
        {
            Assembly a = Assembly.Load(data);
            return a;
        }
    }

    public class Program
    {
        private static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in FileWrite process: {0}", ex);
                return false;
            }
        }
        private static byte[] GetRBLoadNWC(String aslocation)
        {
            using (var client = new System.Net.WebClient())
            {
                return client.DownloadData(aslocation);
            }
        }

        private static byte[] GetRBLoad(String aslocation)
        {
            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            WebRequest asrequest = WebRequest.Create(aslocation);
            asrequest.CachePolicy = noCachePolicy;

            MemoryStream asMemoryStream = new MemoryStream(0x10000);

            Console.WriteLine("Downloading " + aslocation);
            using (Stream asResponseStream = asrequest.GetResponse().GetResponseStream())
            {
                byte[] buffer = new byte[0x1000];
                int bytes;
                while ((bytes = asResponseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    asMemoryStream.Write(buffer, 0, bytes);
                }
            }

            // MemoryStream -> byte[]
            byte[] asbytes = asMemoryStream.ToArray();

            return asbytes;
        }
        private static Assembly GetRALoad(String aslocation)
        {
          
            Assembly assembly = Assembly.Load(GetRBLoad(aslocation));
            return assembly;
        }
        private static void showAssembliesInDomain(AppDomain ad)
        {

            //Make an array for the list of assemblies. (on Disk?)
            Assembly[] assems = ad.GetAssemblies();

            //List the assemblies in the current application domain.
            Console.WriteLine("List of assemblies loaded in current appdomain:");
            foreach (Assembly assem in assems)
                Console.WriteLine(assem.ToString());
        }
        static void Main(string[] args)
        {

            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            AppDomainSetup setup = new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory };
            AppDomain friendlyDomain = AppDomain.CreateDomain("Friendly", null, setup, permissions);


            String[] assemblynames = new String[3] {
                "Microsoft.Dynamic.dll", "Microsoft.Scripting.dll",  "IronPython.dll"
            };

        
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();


            Stage stage = (Stage)friendlyDomain.CreateInstanceAndUnwrap(
                    typeof(Stage).Assembly.FullName, typeof(Stage).FullName);

            foreach (String assemblyname in assemblynames)
            {

                // TODO: Check if downloaded. Do not re-download, do not re-dump to disk

                // Grab from Network
                Console.WriteLine("Grabbing {0}", assemblyname);
                // Byte[] assemblybites = GetRBLoad(String.Concat("http://127.0.0.1:8000/", assemblyname));
                Byte[] assemblybites = GetRBLoadNWC(String.Concat("http://127.0.0.1:8000/", assemblyname));
                
                // Dump to file
                // ByteArrayToFile(assemblyname, assemblybites);

                // Load in memory
                Console.WriteLine("Loading {0} ({1} bytes)", assemblyname, assemblybites.Length);
                // Assembly memassembly = stage.LoadAssembly(assemblybites);
                Assembly assembly = Assembly.Load(assemblybites);

                //assemblies.Add(assemblyname, memassembly);
                assemblies.Add(assemblyname, assembly);

            }

            //showAssembliesInDomain(friendlyDomain);
            showAssembliesInDomain(AppDomain.CurrentDomain);
            Console.WriteLine("Stand successful");
            //showAssembliesInDomain(friendlyDomain);
            
            Type Ipytype = assemblies["IronPython.dll"].GetType("IronPython.Hosting.Python");
            //Type MScrtype = assemblies["Microsoft.Scripting.dll"].GetType("Microsoft.Scripting.Hosting.ScriptEngine");

            Console.WriteLine(Ipytype);
           // Console.WriteLine(MScrtype);

            MethodInfo create = Ipytype.GetMethod("CreateEngine", new Type[] { });

            dynamic pengine = create.Invoke(null, null);
            dynamic scope = pengine.CreateScope();

            dynamic pythonScript =
    pengine.CreateScriptSourceFromString(@"
import clr
print 'Hello World from iPython!'
asnames=[assembly.GetName().Name for assembly in clr.References]
print asnames


");

            pythonScript.Execute(scope);

            // ScriptEngine pengine = (ScriptEngine)create.Invoke(null, null);

            /*ScriptScope scope = pengine.CreateScope();

            ScriptSource pythonScript =
                pengine.CreateScriptSourceFromString(@"
import clr
print 'Hello World from iPython!'
asnames=[assembly.GetName().Name for assembly in clr.References]
print asnames


");
        
            pythonScript.Execute(scope);
           
            ScriptSource pythonScriptFile = pengine.CreateScriptSourceFromFile("hook.py");
            pythonScriptFile.Execute(scope); */

            Console.ReadKey();
        }
    }
}
