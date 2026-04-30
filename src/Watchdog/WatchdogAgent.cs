using Phantom.Testing.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Phantom.Testing.Watchdog
{
    public class WatchdogAgent : IDisposable
    {
        private const int WaitForConnectionTimeout = 1000;
        private const int WaitForReadTimeout = 1000;
        private const int WaitForExitTimeout = 2000;
        private const int WaitForHostReadyRetryCount = 6;
        private const int WaitForHostReadyRetryWaitingTime = 100;

        private readonly string _hostPipeName;
        private readonly ILogger _logger;

        private Process _hostProcess;

        public WatchdogAgent()
        {
            _hostPipeName = WatchdogHostConfiguration.GetPipeName(Process.GetCurrentProcess().Id);
        }

        public WatchdogAgent(ILogger logger) : this()
        {
            _logger = logger;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            if (_hostProcess != null)
            {
                throw new InvalidOperationException("Watchdog already started");
            }

            var process = WatchdogLauncher.Launch(Process.GetCurrentProcess().Id);
            try
            {
                _hostProcess = process;
                WaitForHostReady();
            }
            catch
            {
                process.SafeKill();
                process.Dispose();
                _hostProcess = null;
                throw;
            }
        }

        public void Stop(bool kill = false)
        {
            var command = kill ? "KILL" : "QUIT";

            if (_hostProcess != null)
            {
                try
                {
                    SendCommandWithoutResponse(command);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning($"Failed to send {command} command:{Environment.NewLine}{e}");
                }

                _hostProcess.SafeWaitForExit(WaitForExitTimeout);
                if (_hostProcess.SafeIsActive())
                {
                    _hostProcess.SafeKill();
                }
                _hostProcess.Dispose();
                _hostProcess = null;
            }
        }

        private void WaitForHostReady()
        {
            int tries = 0;
            while (true)
            {
                try
                {
                    var response = SendCommand("PING");
                    if (response == "PONG")
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogDebug($"Failed to contact watchdog host (PING):{Environment.NewLine}{e}");
                }

                tries++;
                if (tries == WaitForHostReadyRetryCount)
                {
                    throw new TimeoutException($"Watchdog host not ready after {tries} retries");
                }

                Thread.Sleep(WaitForHostReadyRetryWaitingTime);
            }
        }

        private void SendCommandWithoutResponse(string command)
        {
            using (var client = new NamedPipeClientStream(".", _hostPipeName, PipeDirection.Out))
            {
                client.Connect(WaitForConnectionTimeout);
                using (var writer = new StreamWriter(client))
                {
                    writer.WriteLine(command);
                    writer.Flush();
                }
            }
        }

        private string SendCommand(string command)
        {
            using (var client = new NamedPipeClientStream(".", _hostPipeName, PipeDirection.InOut))
            {
                client.Connect(WaitForConnectionTimeout);

                using (var reader = new StreamReader(client))
                using (var writer = new StreamWriter(client))
                {
                    writer.WriteLine(command);
                    writer.Flush();

                    var task = reader.ReadLineAsync();
                    if (!task.Wait(WaitForReadTimeout))
                    {
                        throw new TimeoutException("Watchdog response timeout");
                    }
                    return task.Result;
                }
            }
        }

        public bool AddMonitoredProcess(int processId)
        {
            try
            {
                SendCommandWithoutResponse($"REGISTER {processId}");
                return true;
            }
            catch (Exception e)
            {
                _logger?.LogError($"Failed to register monitored process, ProcessId={processId}:{Environment.NewLine}{e}");
            }
            return false;
        }

        public bool RemoveMonitoredProcess(int processId)
        {
            try
            {
                SendCommandWithoutResponse($"UNREGISTER {processId}");
                return true;
            }
            catch (Exception e)
            {
                _logger?.LogError($"Failed to unregister monitored process, ProcessId={processId}:{Environment.NewLine}{e}");
            }
            return false;
        }
    }
}
