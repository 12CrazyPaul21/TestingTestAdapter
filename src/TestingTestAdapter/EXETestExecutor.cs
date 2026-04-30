using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.TestAdapter.Discoverer;
using Phantom.Testing.TestAdapter.Executor;
using Phantom.Testing.TestAdapter.Settings;
using System.Collections.Generic;
using System.Diagnostics;

namespace Phantom.Testing.TestAdapter
{
    [ExtensionUri(Constants.ExecutorUriString)]
    public class EXETestExecutor : ITestExecutor
    {
        private ITestingExecutor _executor;

        public void Cancel()
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            _executor?.Cancel();
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if DEBUG
            if (!runContext.IsBeingDebugged && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            var settings = RunSettings.FromRunSettings(runContext?.RunSettings);
            _executor = TestingRunner.CreateExecutor(settings, runContext, frameworkHandle);
            _executor.RunTests(tests);
        }

        // Trigger: vstest.console.exe <*.exe> /TestAdapterPath:<PATH>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if DEBUG
            if (!runContext.IsBeingDebugged && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            var settings = RunSettings.FromRunSettings(runContext?.RunSettings);
            var discoverer = TestingDiscovererFactory.Create(settings, frameworkHandle);
            var tests = discoverer.DiscoverTests(sources);
            _executor = TestingRunner.CreateExecutor(settings, runContext, frameworkHandle);
            _executor.RunTests(tests);
        }
    }
}
