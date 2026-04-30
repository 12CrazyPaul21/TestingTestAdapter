using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.Win32.SafeHandles;
using Phantom.Testing.Common;
using Phantom.Testing.TestAdapter.Debugging;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Phantom.Testing.TestAdapter.ProcessExecution
{
    internal class DebugProcessLauncher : IProcessLauncher
    {
        private readonly RunSettings _runSettings;
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly ILogger _logger;

        public DebugProcessLauncher(RunSettings settings, IFrameworkHandle frameworkHandle)
        {
            _runSettings = settings;
            _frameworkHandle = frameworkHandle;
            _logger = new MessageLoggerAdapter(frameworkHandle);
        }

        private IProcess SpawnWithManualAttach(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
        {
            DebugProcess process = DebugProcess.Spawn(executable, parameters, WorkingDirectory, environmentVariables);
            var attacher = new MessageBasedDebuggerAttacher(_runSettings.DebuggingNamedPipeId, _frameworkHandle);
            if (!attacher.AttachDebugger(process.Id))
            {
                _logger.LogError($"Could not attach debugger to process {process.Id}");
            }
            return process;
        }

        private IProcess SpawnWithFrameworkAttach(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
        {
            IFrameworkHandle2 frameworkHandle2 = _frameworkHandle as IFrameworkHandle2;
            DebugProcess process = DebugProcess.Spawn(executable, parameters, WorkingDirectory, environmentVariables);
            if (!frameworkHandle2.AttachDebuggerToProcess(process.Id))
            {
                _logger.LogError($"Could not attach debugger to process {process.Id}");
            }
            return process;
        }

        public IProcess Spawn(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
        {
            if (!string.IsNullOrWhiteSpace(_runSettings.DebuggingNamedPipeId))
            {
                return SpawnWithManualAttach(executable, parameters, WorkingDirectory, environmentVariables);
            }

            if (_frameworkHandle is IFrameworkHandle2)
            {
                return SpawnWithFrameworkAttach(executable, parameters, WorkingDirectory, environmentVariables);
            }

            return FrameworkDebugProcess.LaunchWithDebuggerAttached(_frameworkHandle, executable, parameters, WorkingDirectory, environmentVariables);
        }

        private class FrameworkDebugProcess : IProcess
        {
            private readonly Process _process;

            private int _exitCode = -1;
            private bool _exited = false;

            public int Id => _process.Id;
            public bool HasExited => _exited;
            public int ExitCode => _exitCode;
            public string StdOut => string.Empty;
            public string StdErr => string.Empty;

            public FrameworkDebugProcess(Process process)
            {
                _process = process;
                _process.EnableRaisingEvents = true;
                _process.Exited += OnExited;
            }

            public void Dispose()
            {
                _process.Dispose();
            }

            public void WaitForExit()
            {
                lock (this)
                {
                    while (!_exited)
                    {
                        Monitor.Wait(this);
                    }
                }
            }

            private void OnExited(object sender, EventArgs e)
            {
                if (sender is Process process)
                {
                    lock (this)
                    {
                        _exitCode = process.ExitCode;
                        _exited = true;

                        process.Exited -= OnExited;

                        Monitor.Pulse(this);
                    }
                }
            }

            public static FrameworkDebugProcess LaunchWithDebuggerAttached(
                IFrameworkHandle frameworkHandle, string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
            {
                var pid = frameworkHandle.LaunchProcessWithDebuggerAttached(executable, WorkingDirectory, parameters, environmentVariables);
                return new FrameworkDebugProcess(Process.GetProcessById(pid));
            }
        }

        private class DebugProcess : IProcess
        {
            public int Id => _processInfo.dwProcessId;
            public bool HasExited => _exited;
            public int ExitCode => _exitCode;
            public string StdOut => _stdoutCache ?? string.Empty;
            public string StdErr => _stderrCache ?? string.Empty;

            private NativeMethods.ProcessPipeStream _stdoutPipeStream;
            private NativeMethods.ProcessPipeStream _stderrPipeStream;
            private NativeMethods.PROCESS_INFORMATION _processInfo;

            private string _stdoutCache;
            private string _stderrCache;

            private bool _disposed;
            private int _exitCode = -1;
            private bool _exited;

            public DebugProcess()
            {

            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _stdoutPipeStream?.Dispose();
                _stderrPipeStream?.Dispose();

                if (_processInfo.hProcess != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(_processInfo.hProcess);
                }
                if (_processInfo.hThread != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(_processInfo.hThread);
                }

                _disposed = true;
            }

            public void WaitForExit()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DebugProcess));
                }

                using (var process = new SafeWaitHandle(_processInfo.hProcess, true))
                using (var thread = new SafeWaitHandle(_processInfo.hThread, true))
                {
                    _processInfo.hProcess = IntPtr.Zero;
                    _processInfo.hThread = IntPtr.Zero;

                    NativeMethods.ResumeThread(thread);

                    var stdout = new List<string>();
                    var stderr = new List<string>();
                    var threads = new List<Thread>
                    {
                        new Thread(() =>
                        {
                            using (var reader = new StreamReader(_stdoutPipeStream, Encoding.Default))
                            {
                                _stdoutPipeStream = null;
                                while (!reader.EndOfStream)
                                {
                                    stdout.Add(reader.ReadLine());
                                }
                            }
                        }),
                        new Thread(() =>
                        {
                            using (var reader = new StreamReader(_stderrPipeStream, Encoding.Default))
                            {
                                _stderrPipeStream = null;
                                while (!reader.EndOfStream)
                                {
                                    stderr.Add(reader.ReadLine());
                                }
                            }
                        })
                    };
                    foreach (var t in threads)
                    {
                        t.Start();
                    }
                    foreach (var t in threads)
                    {
                        t.Join();
                    }

                    NativeMethods.WaitForSingleObject(process, NativeMethods.INFINITE);

                    if (!NativeMethods.GetExitCodeProcess(process, out int exitCode))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get exit code of process");
                    }

                    _stdoutCache = string.Join(Environment.NewLine, stdout.Where(x => x != null));
                    _stderrCache = string.Join(Environment.NewLine, stderr.Where(x => x != null));
                    _exitCode = exitCode;
                    _exited = true;
                }
            }

            public static DebugProcess Spawn(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
            {
                var stdoutPipeStream = new NativeMethods.ProcessPipeStream();
                var stderrPipeStream = new NativeMethods.ProcessPipeStream();

                NativeMethods.PROCESS_INFORMATION processInfo;

                try
                {
                    processInfo = NativeMethods.CreateProcess(
                        executable, parameters, WorkingDirectory, environmentVariables, stdoutPipeStream._writingEnd, stderrPipeStream._writingEnd);
                }
                catch
                {
                    stdoutPipeStream.Dispose();
                    stderrPipeStream.Dispose();
                    throw;
                }

                stdoutPipeStream.ConnectedToChildProcess();
                stderrPipeStream.ConnectedToChildProcess();

                return new DebugProcess()
                {
                    _stdoutPipeStream = stdoutPipeStream,
                    _stderrPipeStream = stderrPipeStream,
                    _processInfo = processInfo,
                };
            }

            internal static class NativeMethods
            {
                internal class ProcessPipeStream : PipeStream
                {
                    public readonly SafePipeHandle _writingEnd;

                    public ProcessPipeStream() : base(PipeDirection.In, 0)
                    {
                        if (!CreatePipe(out SafePipeHandle readingEnd, out _writingEnd, IntPtr.Zero, 0))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create pipe");
                        }

                        if (!SetHandleInformation(_writingEnd, HANDLE_FLAG_INHERIT, HANDLE_FLAG_INHERIT))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set handle information");
                        }

                        InitializeHandle(readingEnd, false, false);
                    }

                    public void ConnectedToChildProcess()
                    {
                        _writingEnd?.Dispose();
                        IsConnected = true;
                    }

                    protected override void Dispose(bool disposing)
                    {
                        base.Dispose(disposing);
                        if (disposing)
                        {
                            _writingEnd?.Dispose();
                        }
                    }
                }

                private static StringBuilder CreateEnvironment(IDictionary<string, string> environmentVariables)
                {
                    StringDictionary envVariables = new ProcessStartInfo().EnvironmentVariables;

                    if (environmentVariables != null)
                    {
                        foreach (var variable in environmentVariables)
                        {
                            envVariables[variable.Key] = variable.Value;
                        }
                    }

                    var envVariablesList = new List<string>();
                    foreach (DictionaryEntry entry in envVariables)
                    {
                        envVariablesList.Add($"{entry.Key}={entry.Value}");
                    }
                    envVariablesList.Sort();

                    var result = new StringBuilder();
                    foreach (string variable in envVariablesList)
                    {
                        result.Append(variable);
                        result.Length++;
                    }
                    result.Length++;

                    return result;
                }

                internal static PROCESS_INFORMATION CreateProcess(
                    string application, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables,
                    SafePipeHandle stdoutPipeWritingEnd, SafePipeHandle stderrPipeWritingEnd)
                {
                    var startupInfoEx = new STARTUPINFOEX
                    {
                        StartupInfo = new STARTUPINFO
                        {
                            hStdOutput = stdoutPipeWritingEnd,
                            hStdError = stderrPipeWritingEnd,
                            dwFlags = STARTF_USESTDHANDLES,
                            cb = Marshal.SizeOf(typeof(STARTUPINFOEX))
                        }
                    };

                    string commandLine = $"\"{application}\"";
                    if (!string.IsNullOrEmpty(parameters))
                    {
                        commandLine += $" {parameters}";
                    }
                    if (string.IsNullOrWhiteSpace(WorkingDirectory))
                    {
                        WorkingDirectory = null;
                    }

                    if (!CreateProcess(
                        lpApplicationName: application,
                        lpCommandLine: commandLine,
                        lpProcessAttributes: null,
                        lpThreadAttributes: null,
                        bInheritHandles: true,
                        dwCreationFlags: CREATE_EXTENDED_STARTUPINFO_PRESENT | CREATE_SUSPENDED,
                        lpEnvironment: CreateEnvironment(environmentVariables),
                        lpCurrentDirectory: WorkingDirectory,
                        lpStartupInfo: startupInfoEx,
                        lpProcessInformation: out PROCESS_INFORMATION processInfo))
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            $"Failed to create process, command line: '{commandLine}'"
                        );
                    }

                    return processInfo;
                }

                internal const uint CREATE_SUSPENDED = 0x00000004;
                internal const uint CREATE_EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
                internal const uint HANDLE_FLAG_INHERIT = 0x00000001;
                internal const int STARTF_USESTDHANDLES = 0x00000100;
                internal const uint INFINITE = 0xFFFFFFFF;

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                internal class STARTUPINFOEX
                {
                    public STARTUPINFO StartupInfo;
                    public IntPtr lpAttributeList;
                }

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                [BestFitMapping(false, ThrowOnUnmappableChar = true)]
                internal struct STARTUPINFO
                {
                    public Int32 cb;
                    public string lpReserved;
                    public string lpDesktop;
                    public string lpTitle;
                    public Int32 dwX;
                    public Int32 dwY;
                    public Int32 dwXSize;
                    public Int32 dwYSize;
                    public Int32 dwXCountChars;
                    public Int32 dwYCountChars;
                    public Int32 dwFillAttribute;
                    public Int32 dwFlags;
                    public Int16 wShowWindow;
                    public Int16 cbReserved2;
                    public IntPtr lpReserved2;
                    public IntPtr hStdInput;
                    public SafeHandle hStdOutput;
                    public SafeHandle hStdError;
                }

                [StructLayout(LayoutKind.Sequential)]
                internal struct PROCESS_INFORMATION
                {
                    public IntPtr hProcess;
                    public IntPtr hThread;
                    public int dwProcessId;
                    public int dwThreadId;
                }

                [StructLayout(LayoutKind.Sequential)]
                internal class SECURITY_ATTRIBUTES
                {
                    public int nLength;
                    public IntPtr lpSecurityDescriptor;
                    public bool bInheritHandle;
                }

                [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                internal static extern bool CreateProcess(
                    string lpApplicationName,
                    string lpCommandLine,
                    SECURITY_ATTRIBUTES lpProcessAttributes,
                    SECURITY_ATTRIBUTES lpThreadAttributes,
                    bool bInheritHandles,
                    uint dwCreationFlags,
                    [In, MarshalAs(UnmanagedType.LPStr)] StringBuilder lpEnvironment,
                    string lpCurrentDirectory,
                    [In] STARTUPINFOEX lpStartupInfo,
                    out PROCESS_INFORMATION lpProcessInformation);

                [DllImport("kernel32.dll")]
                internal static extern uint ResumeThread(SafeHandle hThread);

                [DllImport("kernel32.dll", SetLastError = true)]
                internal static extern uint WaitForSingleObject(SafeHandle hProcess, uint dwMilliseconds);

                [DllImport("kernel32.dll", SetLastError = true)]
                internal static extern bool GetExitCodeProcess(SafeHandle hProcess, out int lpExitCode);

                [DllImport("kernel32.dll", SetLastError = true)]
                internal static extern bool SetHandleInformation(SafeHandle hObject, uint dwMask, uint dwFlags);

                [DllImport("kernel32.dll", SetLastError = true)]
                internal static extern bool CreatePipe(
                    out SafePipeHandle hReadPipe,
                    out SafePipeHandle hWritePipe,
                    IntPtr securityAttributes,
                    int nSize);

                [DllImport("kernel32.dll", SetLastError = true)]
                internal static extern bool CloseHandle(IntPtr hObject);
            }
        }
    }
}
