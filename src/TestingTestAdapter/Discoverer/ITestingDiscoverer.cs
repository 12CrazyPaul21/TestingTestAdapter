using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    internal interface ITestingDiscoverer
    {
        IEnumerable<TestCase> DiscoverTests(IEnumerable<string> executables);
        IEnumerable<TestCase> DiscoverTests(string executable);
    }
}
