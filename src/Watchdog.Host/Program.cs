using System;

namespace Phantom.Testing.Watchdog.Host
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: Phantom.Testing.Watchdog.Host.exe <Target PID>");
                Environment.Exit(1);
            }

            if (!int.TryParse(args[0], out int targetPid))
            {
                Console.WriteLine($"Error: Invalid PID '{args[0]}'");
                Environment.Exit(1);
            }

            try
            {
#if false
                using (var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole()))
                using (var watchdogHost = new WatchdogHost(targetPid, loggerFactory.CreateLogger<Program>()))
#endif
                using (var watchdogHost = new WatchdogHost(targetPid))
                {
                    watchdogHost.Run();
                }
                Console.WriteLine($"Watchdog for PID {targetPid} exited normally.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal error: {e.Message}");
                Environment.Exit(1);
            }
        }
    }
}
