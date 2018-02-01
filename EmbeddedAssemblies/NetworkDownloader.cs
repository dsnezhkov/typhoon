namespace DynamicShellCode
{

    using System;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;

    public class DynamicShellCodeCompile
    {

        private delegate IntPtr GetPebDelegate();
        public void GetResults()
        {
            this.GetPeb();
        }
        private unsafe IntPtr GetPeb()
        {
            var shellcode = new byte[]
            {

                    0x64, 0xA1, 0x30, 0x00, 0x00, 0x00,         // mov eax, dword ptr fs:[30]
                    0xC3                                        // ret
            };

            MemoryMappedFile mmf = null;
            MemoryMappedViewAccessor mmva = null;
            try
            {
                // Create a read/write/executable memory mapped file to hold our shellcode..
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
            }
            catch
            {
                return IntPtr.Zero;
            }
            finally
            {
                mmva?.Dispose();
                mmf?.Dispose();
            }
        }
    }
}