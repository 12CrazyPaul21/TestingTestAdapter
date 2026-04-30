using System.Collections.Generic;

namespace Phantom.Testing.TestAdapter.ProcessExecution
{
    internal interface IProcessLauncher
    {
        IProcess Spawn(string executable, string parameters, string WorkingDirectory, IDictionary<string, string> environmentVariables);
    }
}
