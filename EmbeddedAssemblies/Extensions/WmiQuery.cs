//https://stackoverflow.com/questions/5406172/utf-8-without-bom
using System;
using System.Reflection;

namespace Cradle.Extensions
{
    // This plugin will implement a contract
    public class WmiQuery
    {

        private Assembly wmi;
        private Type t_cimsession;
        private MethodInfo m_create;
        private dynamic o_cimsession;
        private String host;

        // Can use this to set things up ahead of PreLaunch
        public WmiQuery()
        {
            try
            {
                host = "127.0.0.1";
                // Need to figure out logic to dynamically determine location of such DLLs
                wmi = Assembly.LoadFile(
                    "C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\WMI\\v1.0\\Microsoft.Management.Infrastructure.dll"
                );
                t_cimsession = wmi.GetType("Microsoft.Management.Infrastructure.CimSession");
                m_create = t_cimsession.GetMethod("Create", new Type[] { typeof(string) });
                o_cimsession = m_create.Invoke(null, new object[1] { host });
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to bootstrap WMI to  {0}: {1}", host, e.Message);
            }

        }

        // PreLaunch is executed before RunCode()
        public void PreLaunch() { Console.WriteLine("-- BEGIN -- "); }
        
        // PreLaunch is executed after RunCode()
        public void PostLaunch() { Console.WriteLine("-- END  -- "); }

        private bool Win32_OperatingSystem()
        {
            try
            {
                string Namespace = @"root\cimv2";
                string OSQuery = "SELECT * FROM Win32_OperatingSystem";
                // I don;t know why i need to `winrm config` and start the service for this. I only thought `net start winmgmt
                foreach (var cimInstance in o_cimsession.QueryInstances(Namespace, "WQL", OSQuery))
                {
                    Console.WriteLine("CIM Instance: {0}\n", cimInstance);
                    // or get a specific property: cimInstance.CimInstanceProperties["Name"].Value.ToString());
                    foreach (var cimInstanceProperty in cimInstance.CimInstanceProperties)
                    {
                        Console.WriteLine("\tProperty: {0}", cimInstanceProperty);

                    }
                }
                return true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }
        // Implementing Entry point contract: RunCode()
        public void RunCode()
        {
            Win32_OperatingSystem();
        }
    }
}
