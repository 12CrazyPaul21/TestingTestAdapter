using System.Diagnostics;

namespace Phantom.Testing.Common
{
    public static class ProcessExtensions
    {
        public static void SafeKill(this Process process)
        {
            try { process?.Kill(); } catch { }
        }

        public static void SafeWaitForExit(this Process process, int milliseconds)
        {
            try { process?.WaitForExit(milliseconds); } catch { }
        }

        public static bool SafeIsActive(this Process process)
        {
            try
            {
                return !process.HasExited;
            }
            catch
            {

            }
            return false;
        }
    }
}
