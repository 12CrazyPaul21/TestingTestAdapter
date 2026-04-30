using System.Diagnostics;
using System.IO;

namespace Phantom.Testing.Watchdog
{
    public static class WatchdogLauncher
    {
        public static Process Launch(int processId)
        {
            var hostPath = Path.ChangeExtension(typeof(WatchdogLauncher).Assembly.Location, ".Host.exe");
            return Process.Start(new ProcessStartInfo(hostPath, processId.ToString())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
    }
}
