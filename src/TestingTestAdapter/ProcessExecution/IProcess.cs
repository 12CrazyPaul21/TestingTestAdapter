using System;

namespace Phantom.Testing.TestAdapter.ProcessExecution
{
    internal interface IProcess : IDisposable
    {
        int Id { get; }
        bool HasExited { get; }
        int ExitCode { get; }
        string StdOut { get; }
        string StdErr { get; }

        void WaitForExit();
    }
}
