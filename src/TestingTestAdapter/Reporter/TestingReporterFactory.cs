using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.TestAdapter.Settings;

namespace Phantom.Testing.TestAdapter.Reporter
{
    internal static class TestingReporterFactory
    {
        internal static ITestingReporter Create(TestCase test, RunSettings settings, IFrameworkHandle frameworkHandle)
        {
            if (settings?.ReporterUseWrapper == true)
            {
                return new TestingExternalReporter(test, settings, frameworkHandle);
            }
            else
            {
                return new TestingReporter(test, frameworkHandle);
            }
        }
    }
}
