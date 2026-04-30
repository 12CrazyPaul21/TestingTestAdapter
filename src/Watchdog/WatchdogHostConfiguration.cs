namespace Phantom.Testing.Watchdog
{
    public static class WatchdogHostConfiguration
    {
        public static string GetPipeName(int processId)
        {
            return $"Phantom.Testing.Watchdog.pipe.{processId}";
        }
    }
}
