import clr
clr.AddReference("System")
clr.AddReference("System.IO.Compression")
from System.Net import WebClient
from System.Net import ServicePointManager, SecurityProtocolType
from System.IO import Stream, StreamReader, MemoryStream, BinaryWriter, FileStream, File
from System.IO.Compression import ZipArchive, ZipArchiveMode


with WebClient() as wc:
    ServicePointManager.SecurityProtocol  = SecurityProtocolType.Tls12
    # dll = wc.DownloadData("https://github.com/jduv/AppDomainToolkit/blob/master/AppDomainToolkit.UnitTests/test-assembly-files/TestWithNoReferences.dll")

from System import AppDomain, AppDomainSetup, MarshalByRefObject
from System.Environment import CurrentDirectory 
from System.IO import Path, Directory 
from System.Reflection import Assembly 

assemblyName="tmpB1B9.dll"

# Byte loading, allowed in DLR, disallowed in .Net Classic, allowed in Core??
clr.AddReference(Assembly.Load(File.ReadAllBytes(assemblyName)))
from Typhoon.Extensions import ClipboardManager

cm = ClipboardManager()
cm.RunCode()

#clr.AddReference(Assembly.Load(r))
