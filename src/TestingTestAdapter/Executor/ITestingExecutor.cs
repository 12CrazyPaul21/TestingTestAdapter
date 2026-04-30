using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Collections.Generic;

namespace Phantom.Testing.TestAdapter.Executor
{
    internal interface ITestingExecutor
    {
        void Cancel();
        void RunTests(IEnumerable<TestCase> tests);
    }
}
