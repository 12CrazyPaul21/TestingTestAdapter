using Microsoft.Dia;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Phantom.Testing.TestAdapter.Dia
{
    internal static class DiaFactory
    {
        private const string Msdia140Dll = "msdia140.dll";

        [ThreadStatic]
        private static IClassFactory DiaSourceFactory;

        private static readonly string MsdiaDllPath;
        private static readonly IntPtr MsdiaDll;
        private static readonly int MsdiaDllLoadError;

        static DiaFactory()
        {
            MsdiaDllPath = Path.Combine(GetMsidaAssemblyBaseDir(), Msdia140Dll);
            MsdiaDll = NativeMethods.LoadLibrary(MsdiaDllPath);
            MsdiaDllLoadError = (MsdiaDll == IntPtr.Zero) ? Marshal.GetLastWin32Error() : 0;
        }

        public static IDiaDataSource CreateDiaDataSource()
        {
            var IID_IDiaDataSource = typeof(IDiaDataSource).GUID;
            return (IDiaDataSource)CreateInstance(ref IID_IDiaDataSource);
        }

        private static object CreateInstance(ref Guid iid)
        {
            if (MsdiaDll == IntPtr.Zero)
            {
                throw new Win32Exception(MsdiaDllLoadError, $"Failed to load ${MsdiaDllPath}");
            }

            if (DiaSourceFactory == null)
            {
                var DiaSourceClassGuid = new Guid("e6756135-1e65-4d17-8576-610761398c3c");
                var IID_IClassFactory = typeof(IClassFactory).GUID;
                DiaSourceFactory = (IClassFactory)NativeMethods.DllGetClassObject(ref DiaSourceClassGuid, ref IID_IClassFactory);
            }

            return DiaSourceFactory.CreateInstance(null, iid);
        }

        private static string GetMsidaAssemblyBaseDir()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            return Path.Combine(path, "Dia2Lib", Environment.Is64BitProcess ? "x64" : "x86");
        }

        private static class NativeMethods
        {
            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string path);

            [DllImport(Msdia140Dll, ExactSpelling = true, PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface)]
            public static extern object DllGetClassObject([In] ref Guid clsid, [In] ref Guid iid);
        }

        [ComImport]
        [ComVisible(false)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("00000001-0000-0000-C000-000000000046")]
        private interface IClassFactory
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object CreateInstance([MarshalAs(UnmanagedType.IUnknown)] object outer, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);
            void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
        }
    }
}
