using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.TestAdapter.Discoverer;
using Phantom.Testing.TestAdapter.Settings;
using System.Collections.Generic;
using System.Diagnostics;

namespace Phantom.Testing.TestAdapter
{
    [FileExtension(".exe")]
    [DefaultExecutorUri(Constants.ExecutorUriString)]
    public class EXETestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            var settings = RunSettings.FromRunSettings(discoveryContext?.RunSettings);
            var discoverer = TestingDiscovererFactory.Create(settings, logger);
            var tests = discoverer.DiscoverTests(sources);
            foreach (var test in tests)
            {
                discoverySink.SendTestCase(test);
            }
        }
    }
}
