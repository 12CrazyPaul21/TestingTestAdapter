using Phantom.Testing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace Phantom.Testing.Watchdog
{
    public class WatchdogHost : IDisposable
    {
        private const int TargetProcessCheckIntervalMs = 1000;

        private readonly int _targetPid;
        private readonly string _pipeName;
        private readonly ILogger _logger;

        private readonly HashSet<int> _monitoredProcesses = new HashSet<int>();
        private readonly object _lock = new object();

        private delegate string CommandHandler(string arg);
        private readonly Dictionary<string, CommandHandler> _handlers;

        private readonly ManualResetEventSlim _quitEvent = new ManualResetEventSlim(false);

        private volatile bool _normalExit;
        private bool _disposed;

        public WatchdogHost(int targetPid) : this(targetPid, null)
        {

        }

        public WatchdogHost(int targetPid, ILogger logger)
        {
            _targetPid = targetPid;
            _pipeName = WatchdogHostConfiguration.GetPipeName(_targetPid);
            _logger = logger;
            _handlers = BuildCommandHandlers();
        }

        private Dictionary<string, CommandHandler> BuildCommandHandlers()
        {
            return new Dictionary<string, CommandHandler>
            {
                ["QUIT"] = _ =>
                {
                    _normalExit = true;
                    _quitEvent.Set();
                    return null;
                },
                ["KILL"] = _ =>
                {
                    _quitEvent.Set();
                    return null;
                },
                ["PING"] = _ => "PONG",
                ["REGISTER"] = arg =>
                {
                    if (int.TryParse(arg, out var pid))
                    {
                        AddMonitoredProcess(pid);
                    }
                    return null;
                },
                ["UNREGISTER"] = arg =>
                {
                    if (int.TryParse(arg, out var pid))
                    {
                        RemoveMonitoredProcess(pid);
                    }
                    return null;
                }
            };
        }

        private void AddMonitoredProcess(int processId)
        {
            lock (_lock)
            {
                _monitoredProcesses.Add(processId);
            }
        }

        private void RemoveMonitoredProcess(int processId)
        {
            lock (_lock)
            {
                _monitoredProcesses.Remove(processId);
            }
        }

        private string HandleCommand(string command)
        {
            CommandHandler handler;
            var space = command.IndexOf(' ');

            if (space < 0)
            {
                return _handlers.TryGetValue(command, out handler) ? handler(null) : null;
            }

            var cmd = command.Substring(0, space);
            var arg = command.Substring(space + 1);

            return _handlers.TryGetValue(cmd, out handler) ? handler(arg) : null;
        }

        private void PipeLoop()
        {
            while (!_quitEvent.IsSet)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(
                        pipeName: _pipeName,
                        direction: PipeDirection.InOut,
                        maxNumberOfServerInstances: 1,
                        transmissionMode: PipeTransmissionMode.Byte,
                        options: PipeOptions.Asynchronous))
                    {
                        var asyncResult = server.BeginWaitForConnection(null, null);

                        WaitHandle.WaitAny(new[]
                        {
                            _quitEvent.WaitHandle,
                            asyncResult.AsyncWaitHandle
                        });

                        if (_quitEvent.IsSet)
                        {
                            break;
                        }

                        server.EndWaitForConnection(asyncResult);

                        using (var reader = new StreamReader(server, Encoding.UTF8, false, 1024, true))
                        using (var writer = new StreamWriter(server, Encoding.UTF8, 1024, true))
                        {
                            var response = HandleCommand(reader.ReadLine());
                            if (response != null)
                            {
                                writer.WriteLine(response);
                                writer.Flush();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError($"WatchdogHost PipeLoop error:{Environment.NewLine}{e}");
                }
            }
        }

        public void Run()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WatchdogHost));
            }

            var pipeThread = new Thread(PipeLoop)
            {
                IsBackground = true
            };
            pipeThread.Start();

            while (!_quitEvent.IsSet)
            {
                if (!IsTargetProcessAlive())
                {
                    _quitEvent.Set();
                    break;
                }

                Thread.Sleep(TargetProcessCheckIntervalMs);
            }

            pipeThread.Join();
        }

        private bool IsTargetProcessAlive()
        {
            try
            {
                using (var process = Process.GetProcessById(_targetPid))
                {
                    return !process.HasExited;
                }
            }
            catch
            {
                return false;
            }
        }

        private void TerminateMonitoredProcesses()
        {
            List<int> snapshot;

            lock (_lock)
            {
                snapshot = _monitoredProcesses.ToList();
            }

            foreach (var pid in snapshot)
            {
                try
                {
                    using (var process = Process.GetProcessById(pid))
                    {
                        process.Kill();
                    }
                }
                catch
                {

                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_normalExit)
                {
                    TerminateMonitoredProcesses();
                }

                _quitEvent.Set();
                _disposed = true;
            }
        }
    }
}
