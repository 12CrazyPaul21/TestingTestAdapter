using Phantom.Testing.Watchdog;
using System.Diagnostics;

namespace Watchdog.Tests
{
    public class WatchdogTests
    {
        private static Process StartCmdTimeout()
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c timeout /t 15",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        [Fact]
        public void Watchdog_ShouldMonitorRegisteredProcess()
        {
            var p1 = StartCmdTimeout();
            var p2 = StartCmdTimeout();

            using var watchdog = new WatchdogAgent();
            watchdog.Start();
            watchdog.AddMonitoredProcess(p1.Id);
            watchdog.AddMonitoredProcess(p2.Id);
            watchdog.RemoveMonitoredProcess(p2.Id);
            watchdog.Stop(true);

            Assert.True(p1.WaitForExit(2000), $"ProcessId({p1.Id})");
            Assert.False(p2.WaitForExit(2000), $"ProcessId({p2.Id})");
        }
    }
}
