using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Phantom.Testing.TestAdapter.ProcessExecution
{
    internal class TestProcessLauncher : IProcessLauncher
    {
        private class TestProcess : IProcess
        {
            private readonly Process _process;
            private readonly List<string> _stdout = new List<string>();
            private readonly List<string> _stderr = new List<string>();

            private string _stdoutCache;
            private string _stderrCache;

            public int Id => _process.Id;
            public bool HasExited => _process.HasExited;
            public int ExitCode => _process.ExitCode;
            public string StdOut => _stdoutCache ?? string.Empty;
            public string StdErr => _stderrCache ?? string.Empty;

            private TestProcess(Process process)
            {
                _process = process;
            }

            public void Dispose()
            {
                _process?.Dispose();
            }

            private void Start()
            {
                _process.OutputDataReceived += (sender, e) =>
                {
                    _stdout.Add(e.Data);
                };
                _process.ErrorDataReceived += (sender, e) =>
                {
                    _stderr.Add(e.Data);
                };

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }

            public void WaitForExit()
            {
                _process.WaitForExit();

                _stdoutCache = string.Join(Environment.NewLine, _stdout.Where(x => x != null));
                _stderrCache = string.Join(Environment.NewLine, _stderr.Where(x => x != null));
            }

            public static TestProcess Spawn(ProcessStartInfo processStartInfo)
            {
                var process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };

                var testProcess = new TestProcess(process);
                testProcess.Start();

                return testProcess;
            }
        }

        private readonly RunSettings _runSettings;
        private readonly IFrameworkHandle _frameworkHandle;

        public TestProcessLauncher(RunSettings settings, IFrameworkHandle frameworkHandle)
        {
            _runSettings = settings;
            _frameworkHandle = frameworkHandle;
        }

        public IProcess Spawn(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables)
        {
            var processStartInfo = new ProcessStartInfo(executable, parameters)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory,
            };

            if (environmentVariables != null)
            {
                foreach (var variable in environmentVariables)
                {
                    processStartInfo.Environment[variable.Key] = variable.Value;
                }
            }

            return TestProcess.Spawn(processStartInfo);
        }
    }
}
