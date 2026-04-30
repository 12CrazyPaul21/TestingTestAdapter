using System.Collections.Generic;

namespace Phantom.Testing.TestAdapter.Settings
{
    public interface ITestingTestAdapterSettings
    {
        string DebuggingNamedPipeId { get; set; }
        DiscovererSettings Discoverer { get; set; }
        ReporterSettings Reporter { get; set; }
        bool? WatchdogDisabled { get; set; }
        string WorkingDirectory { get; set; }
        string PathExtension { get; set; }
        List<EnvironmentVariable> EnvironmentVariables { get; set; }
    }
}
