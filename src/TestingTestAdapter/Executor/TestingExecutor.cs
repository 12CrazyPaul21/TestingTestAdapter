using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.Common;
using Phantom.Testing.TestAdapter.ProcessExecution;
using Phantom.Testing.TestAdapter.Settings;
using Phantom.Testing.Watchdog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Phantom.Testing.TestAdapter.Executor
{
    internal class TestingExecutor : ITestingExecutor
    {
        private readonly RunSettings _settings;
        private readonly IRunContext _runContext;
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly ILogger _logger;
        private readonly bool _useWatchdog;
        private readonly object _lock = new object();
        private readonly IProcessLauncher _launcher;

        private bool _canceled;

        public TestingExecutor(RunSettings settings, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _settings = settings;
            _runContext = runContext;
            _frameworkHandle = frameworkHandle;
            _logger = new MessageLoggerAdapter(frameworkHandle);
            _useWatchdog = _settings?.WatchdogDisabled == false;
            _launcher = new TestProcessLauncher(settings, frameworkHandle);
        }

        private (int pid, Task task) RunTest(TestCase test)
        {
            var (process, reporter) = TestingRunner.RunTest(test, _launcher, _settings, _frameworkHandle);
            var task = Task.Run(() =>
            {
                TestingRunner.WaitAndReportTestExecution(process, reporter);
                process.Dispose();
            });
            return (process.Id, task);
        }

        public async Task RunTestsAsync(IEnumerable<TestCase> tests)
        {
            using (var watchdog = _useWatchdog ? new WatchdogAgent(_logger) : null)
            {
                watchdog?.Start();

                var tasks = new Dictionary<Task, int>();

                foreach (var test in tests)
                {
                    lock (_lock)
                    {
                        if (_canceled)
                        {
                            break;
                        }
                    }

                    try
                    {
                        var (pid, task) = RunTest(test);
                        tasks.Add(task, pid);
                        watchdog?.AddMonitoredProcess(pid);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError($"Test scheduling failed: '{test.DisplayName}'{Environment.NewLine}{e}");
                    }
                }

                while (tasks.Count > 0)
                {
                    var snapshot = tasks.Keys.ToArray();
                    var completedTask = await Task.WhenAny(snapshot);
                    var pid = tasks[completedTask];
                    tasks.Remove(completedTask);
                    watchdog?.RemoveMonitoredProcess(pid);

                    try
                    {
                        await completedTask;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError($"Test execution failed: {Environment.NewLine}{e}");
                    }
                }

                watchdog?.Stop();
            }
        }

        public void Cancel()
        {
            lock (_lock)
            {
                if (_canceled)
                {
                    return;
                }

                _canceled = true;
                _logger.LogInfo("Test execution canceled");
            }
        }

        [SuppressMessage("Usage", "VSTHRD002")]
        [SuppressMessage("Style", "IDE0079")]
        public void RunTests(IEnumerable<TestCase> tests)
        {
            // ThreadHelper.JoinableTaskFactory.Run(() => RunTestsAsync(tests));
            RunTestsAsync(tests).GetAwaiter().GetResult();
            Debug.WriteLine("All test done");
        }
    }
}
