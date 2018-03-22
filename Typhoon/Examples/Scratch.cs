using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon
{
    class Scratch
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(
                  string lpszUsername,
                  string lpszDomain,
                  IntPtr lpPassword,
                  int dwLogonType,
                  int dwLogonProvider,
                  out IntPtr phToken
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

            enum TOKEN_INFORMATION_CLASS {
                TokenUser = 1,
                TokenGroups,
                TokenPrivileges,
                TokenOwner,
                TokenPrimaryGroup,
                TokenDefaultDacl,
                TokenSource,
                TokenType,
                TokenImpersonationLevel,
                TokenStatistics,
                TokenRestrictedSids,
                TokenSessionId,
                TokenGroupsAndPrivileges,
                TokenSessionReference,
                TokenSandBoxInert,
                TokenAuditPolicy,
                TokenOrigin
                };

        private static void GetNTAcctForPath(String path)
        {
            FileSecurity fs = File.GetAccessControl(path);
            AuthorizationRuleCollection ntarc = fs.GetAccessRules(true, true, typeof(NTAccount));
            AuthorizationRuleCollection siarc = fs.GetAccessRules(true, true, typeof(SecurityIdentifier));

            Console.WriteLine("Checking SIDs on path: {0}", path);

            Console.WriteLine("\n== By NTAccount ==");
            foreach (FileSystemAccessRule fsar in ntarc)
            {
                NTAccount nta = (NTAccount)fsar.IdentityReference;
                Console.WriteLine("{0} <=> {1}", nta.Value, nta.IsValidTargetType(typeof(SecurityIdentifier)) ?
                                                nta.Translate(typeof(SecurityIdentifier)).ToString() :
                                                "-");
            }
            Console.WriteLine("\n== By SecurityIdentifier == ");
            foreach (FileSystemAccessRule siar in siarc)
            {
                SecurityIdentifier sia = (SecurityIdentifier)siar.IdentityReference;

                try
                {
                    Console.WriteLine("SDDL:{0} <=> {1}",
                                            sia.Value,
                                            (sia.IsAccountSid() ? (sia.IsValidTargetType(typeof(NTAccount)) ?
                                                sia.Translate(typeof(NTAccount)).ToString() : "-") :
                                                "Not a real Windows Account"));

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: {0}", e.Message);
                }
            }
        }

        private static void Showtoken()
        {
            
        }

    public static void Main()
        {
            //GetNTAcctForPath(@"Scratch.exe");
            // bool currentDomain = true; // true  - domain , false - local
            // GetWellKnownSid(currentDomain);
            Process cProc = Process.GetCurrentProcess();
            Console.WriteLine("Process ID: {0}", cProc.Id);
            Console.WriteLine("Process Name: {0}", cProc.ProcessName);
            //ImpersonateUser("Administrator", null);
        }

        

        private static void ImpersonateUser(String userName, String domainName)
        {
            if (domainName == null)
            {
                domainName = ".";
            }
            IntPtr tokenHandle = WindowsIdentity.GetCurrent().Token;

            Console.WriteLine("Before Impersonation");
            Console.WriteLine("Token {0}", tokenHandle);
            Console.WriteLine("Auth type: {0}", WindowsIdentity.GetCurrent().AuthenticationType);
            Console.WriteLine("Impersonation Level: {0}", WindowsIdentity.GetCurrent().ImpersonationLevel);
            Console.WriteLine("Owner of Process: {0}", WindowsIdentity.GetCurrent().Owner);
            Console.WriteLine("User: {0}", WindowsIdentity.GetCurrent().User);

            IntPtr phToken = IntPtr.Zero;

            SecureString pwd = new SecureString();
            // Use the AppendChar method to add each char value to the secure string.

            Console.Write("PW: ");
            char[] che = Console.ReadLine().TrimEnd(Environment.NewLine.ToCharArray()).ToCharArray();
            foreach (char ch in che)
            {
                pwd.AppendChar(ch);
            }

            IntPtr pwdPtr = Marshal.SecureStringToGlobalAllocUnicode(pwd);
            bool status;
            status = LogonUser(userName, domainName, pwdPtr, 3, 3, out phToken);
            pwd.Dispose();

            if (status)
            {
                Console.WriteLine("Impersonating token {0} with token {1}", tokenHandle, phToken);
                try
                {
                    using (WindowsImpersonationContext wic = WindowsIdentity.Impersonate(phToken))
                    {
                        SecurityIdentifier ownerSid = WindowsIdentity.GetCurrent().Owner;
                        SecurityIdentifier userSid = WindowsIdentity.GetCurrent().User;

                        Console.WriteLine("After Impersonation");
                        Console.WriteLine("Token {0}", WindowsIdentity.GetCurrent().Token);
                        Console.WriteLine("|-Name {0}", WindowsIdentity.GetCurrent().Name);
                        Console.WriteLine("|-Auth type: {0}", WindowsIdentity.GetCurrent().AuthenticationType);
                        Console.WriteLine("|-Impersonation Level: {0}", WindowsIdentity.GetCurrent().ImpersonationLevel);
                        Console.WriteLine("|-Owner of Process: {0} {1}", ownerSid,
                                (ownerSid.IsValidTargetType(typeof(NTAccount)) ?
                                    ownerSid.Translate(typeof(NTAccount)).ToString() : "-")
                            );
                        Console.WriteLine("|-User: {0} {1}", userSid,
                                (userSid.IsValidTargetType(typeof(NTAccount)) ?
                                    userSid.Translate(typeof(NTAccount)).ToString() : "-")
                                );
                    }

                }
                catch (Exception e)
                {

                    Console.WriteLine("Impersonation failed: {0}", e.Message);
                }
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                if (err != 0)
                {
                    Console.Write("Unable to Impersonate");
                    Console.WriteLine(" : {0}  - {1} ", err, new Win32Exception(err).Message);
                }
            }
        }

        private static void GetWellKnownSid(bool domain)
        {
            SecurityIdentifier currDomain = null;
            if (domain)
            {
                currDomain = WindowsIdentity.GetCurrent().User.AccountDomainSid;
                Console.WriteLine("\n+++ Domain SID: {0} +++", currDomain);
            }


            foreach (WellKnownSidType wsid in Enum.GetValues(typeof(WellKnownSidType)))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("{0}: ", wsid);
                Console.ResetColor();

                try
                {
                    SecurityIdentifier sid = new SecurityIdentifier(wsid, domain ? new SecurityIdentifier(currDomain.Value) : null);

                    try
                    {
                        Console.WriteLine("{0} , {1} ",
                                (sid.IsValidTargetType(typeof(NTAccount)) ? sid.Translate(typeof(NTAccount)).ToString() : "-"),
                                sid.Value
                            );

                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(" !{0}", e.Message);
                        Console.ResetColor();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine(" !{0}", e.Message);
                    Console.ResetColor();
                }
            }
        }
    }
}

