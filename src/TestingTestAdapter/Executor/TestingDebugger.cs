using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.Common;
using Phantom.Testing.TestAdapter.ProcessExecution;
using Phantom.Testing.TestAdapter.Settings;
using System.Collections.Generic;

namespace Phantom.Testing.TestAdapter.Executor
{
    internal class TestingDebugger : ITestingExecutor
    {
        private readonly RunSettings _settings;
        private readonly IRunContext _runContext;
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private readonly IProcessLauncher _launcher;

        private bool _canceled;

        public TestingDebugger(RunSettings settings, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _settings = settings;
            _runContext = runContext;
            _frameworkHandle = frameworkHandle;
            _logger = new MessageLoggerAdapter(frameworkHandle);
            _launcher = new DebugProcessLauncher(settings, frameworkHandle);
        }

        private void DebugTest(TestCase test)
        {
            var (process, reporter) = TestingRunner.RunTest(test, _launcher, _settings, _frameworkHandle);
            using (process)
            {
                TestingRunner.WaitAndReportTestExecution(process, reporter);
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
                _logger.LogInfo("Test debugging canceled");
            }
        }

        public void RunTests(IEnumerable<TestCase> tests)
        {
            foreach (var test in tests)
            {
                lock (_lock)
                {
                    if (_canceled)
                    {
                        break;
                    }
                }

                DebugTest(test);
            }
        }
    }
}
