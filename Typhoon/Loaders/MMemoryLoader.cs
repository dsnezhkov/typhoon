using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typhoon
{
    /// <summary>
    /// This is what Mapped Memory record looks like
    /// </summary>
    class MMRecord
    {

        public string ResourceType { get; set; }
        public Int64 ResourceSize { get; set; }
        public MemoryMappedFile ResourceMMFile { get; set; }

        public MMRecord(String rType, Int64 rSize, MemoryMappedFile mmf)
        {
            ResourceType = rType;
            ResourceSize = rSize;
            ResourceMMFile = mmf;
        }

    }

    internal static class MMemoryLoader
    {
        private static Dictionary<String, MMRecord> mmfRepo =
                     new Dictionary<String, MMRecord>();

        public static Dictionary<String, MMRecord>  GetMMFRepo()
        {
            return mmfRepo;
        }

        /// <summary>
        /// Print Mapped Memory Map
        /// </summary>
        public static void PrintMemoryMap()
        {
            foreach (KeyValuePair<String, MMRecord> mmfentry in mmfRepo)
            {
                Console.WriteLine("[{0}] ({1}) size:{2}b", mmfentry.Key, mmfentry.Value.ResourceType, mmfentry.Value.ResourceSize);
            }
        }

        /// <summary>
        /// Return contents of memory Location as tring (ASCII or UTF-8)
        /// </summary>
        /// <param name="mmLocation"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static String MMRecordContentString(String mmLocation, String encoding="")
        {

            byte[] mmResourceBytes = MMRecordContentBytes(mmLocation);
            if (encoding.Equals("utf8".ToLower()))
            {
                return Encoding.UTF8.GetString(mmResourceBytes, 0, mmResourceBytes.Length );
            }else
            {
                return Encoding.Default.GetString(mmResourceBytes, 0, mmResourceBytes.Length );
            }
        }            

        /// <summary>
        /// Return conents of Memory Mapped locaton as bytes[]
        /// </summary>
        /// <param name="mmLocation"></param>
        /// <returns></returns>
        public static byte[] MMRecordContentBytes(String mmLocation)
        {

            byte[] mmrResourceBytes = new byte[0] { };
            if (mmfRepo.ContainsKey(mmLocation))
            {
                MMRecord mmr = mmfRepo[mmLocation];

                using (MemoryMappedViewStream mmfstr = mmr.ResourceMMFile.CreateViewStream())
                {
                    BinaryReader mmfreader = new BinaryReader(mmfstr);
                    mmrResourceBytes = mmfreader.ReadBytes((int)mmr.ResourceSize);
                }
            }
            else
            {
                Console.WriteLine("Unable to find {0}. Valid value?", mmLocation);
            }
            return  mmrResourceBytes;
            
        }
        
        /// <summary>
        /// Dumps Meta and contents of the Memory record
        /// </summary>
        /// <param name="mmLocation"></param>
        public static void DumpMMRecord(String mmLocation)
        {
            if (mmfRepo.ContainsKey(mmLocation))
            {
                MMRecord mmr = mmfRepo[mmLocation];
                Console.WriteLine("Location: {0}", mmLocation);
                Console.WriteLine("Type    : {0}", mmr.ResourceType);
                Console.WriteLine("Size    : {0}", mmr.ResourceSize);
                Console.WriteLine("Content : ");

                using (MemoryMappedViewStream mmstream = mmr.ResourceMMFile.CreateViewStream())
                {
                    mmstream.Seek(0, SeekOrigin.Begin);
                    BinaryReader mmfreader = new BinaryReader(mmstream);
                    byte[] mmrResourceBytes = mmfreader.ReadBytes((int)mmr.ResourceSize);
                    //Console.WriteLine("MMRecord bytes: {0}", Encoding.Default.GetString(mmrResourceBytes));
                    Console.WriteLine("MMRecord bytes: {0}", Encoding.UTF8.GetString(mmrResourceBytes));
                }


            }
            else
            {
                Console.WriteLine("Unable to dump {0}. Valid value?", mmLocation);
            }

        }


        /// <summary>
        /// Remove Record from Mapped Memory
        /// </summary>
        /// <param name="mmLocation"></param>
        public static void RemoveMMRecord(String mmLocation)
        {
            if (mmfRepo.ContainsKey(mmLocation))
            {
                MMRecord mmr;
                if (mmfRepo.TryGetValue(mmLocation, out mmr))
                {
                    Console.WriteLine("{0}, {1}, {2}", mmr.ResourceSize, mmr.ResourceType, mmr.ResourceMMFile );
                    try
                    {
                        mmr.ResourceMMFile.SafeMemoryMappedFileHandle.Close();
                        mmr.ResourceMMFile.Dispose();
                        mmfRepo.Remove(mmLocation);

                    }catch(ArgumentNullException ane)
                    {
                        Console.WriteLine("Unable to unload {0}. {1}", mmLocation, ane.Message);
                    }
                }else
                {
                    Console.WriteLine("Unable to unload {0}. Valid value?", mmLocation);
                }
            }else
            {
                Console.WriteLine("Unable to unload {0}. Valid value?", mmLocation );

            }
        }

        /// <summary>
        /// Load Contents of File into Memory Mapped Record
        /// </summary>
        /// <param name="fPath"></param>
        /// <param name="fType"></param>
        public static void LoadMMRecordFromFile(String fPath, String fType)
        {

            String fName = Path.GetFileName(fPath);

            if (!mmfRepo.ContainsKey(fName))
            {
                FileInfo fi = new FileInfo(fPath);
                MemoryMappedFile mmf = MemoryMappedFile.CreateNew(fName, fi.Length, MemoryMappedFileAccess.ReadWrite);
                using (MemoryMappedViewStream mmstream = mmf.CreateViewStream())
                {
                    mmstream.Seek(0, SeekOrigin.Begin);
                    BinaryWriter writer = new BinaryWriter(mmstream);
                    writer.Write(File.ReadAllBytes(fPath));
                }

                // Save mmf in Memory Mapped Repo
                mmfRepo.Add(fName, new MMRecord(fType, fi.Length, mmf));

            }
            else
            {
                Console.WriteLine("Duplicate. File is already loaded at memory segment ({0}). Delete it first.", fName);
            }

        }
    }
}
