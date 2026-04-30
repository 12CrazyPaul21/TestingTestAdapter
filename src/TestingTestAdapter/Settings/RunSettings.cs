using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Phantom.Testing.TestAdapter.Settings
{
    internal class WrapperInfo
    {
        public string WrapperPath { get; private set; }
        public bool IsPythonWrapper { get; private set; }
        public string Python { get; private set; }

        private WrapperInfo(string wrapper)
        {
            WrapperPath = wrapper;
            IsPythonWrapper = string.Equals(Path.GetExtension(WrapperPath), ".py", StringComparison.OrdinalIgnoreCase);
            if (IsPythonWrapper)
            {
                Python = PathUtils.FindPythonInPath();
            }
        }

        public string GetExecutable()
        {
            return IsPythonWrapper ? Python : WrapperPath;
        }

        public string GetArguments(string arguments)
        {
            return IsPythonWrapper ? $"\"{WrapperPath}\" {arguments}" : arguments;
        }

        public static WrapperInfo FindWrapper(RunSettings settings, string wrapper)
        {
            if (string.IsNullOrWhiteSpace(wrapper))
            {
                throw new InvalidOperationException(
                    "Wrapper was not provided."
                );
            }

            if (Path.IsPathRooted(wrapper))
            {
                if (File.Exists(wrapper))
                {
                    return new WrapperInfo(wrapper);
                }

                throw new FileNotFoundException(
                    $"Wrapper not found at absolute path: {wrapper}"
                );
            }

            var searchPaths = new List<string>()
            {
                settings.SolutionDirectory,
                Path.GetDirectoryName(settings.ResultsDirectory),
                Environment.CurrentDirectory,
                AppContext.BaseDirectory,
            }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(p => Path.GetFullPath(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            .Distinct();

            var wrapperPath = PathUtils.FindExecutable(wrapper, searchPaths);
            if (string.IsNullOrWhiteSpace(wrapperPath))
            {
                throw new FileNotFoundException($"Wrapper not found: {wrapper}");
            }

            return new WrapperInfo(wrapperPath);
        }
    }

    internal class RunSettings
    {
        private readonly XDocument _runSettings;
        private readonly TestingRunSettings _testingRunSettings;

        private XElement RunConfiguration => _runSettings?.Root?.Element("RunConfiguration");
        private DiscovererSettings DiscovererSettings => _testingRunSettings?.Discoverer;
        private ReporterSettings ReporterSettings => _testingRunSettings?.Reporter;

        public string SolutionDirectory => RunConfiguration?.Element("SolutionDirectory")?.Value;
        public string ResultsDirectory => RunConfiguration?.Element("ResultsDirectory")?.Value;

        public string DebuggingNamedPipeId => _testingRunSettings?.DebuggingNamedPipeId;
        public bool DiscovererUseWrapper => DiscovererSettings?.UseWrapper == true;
        public WrapperInfo DiscovererWrapper => WrapperInfo.FindWrapper(this, DiscovererSettings?.Wrapper);
        public bool ReporterUseWrapper => ReporterSettings?.UseWrapper == true;
        public WrapperInfo ReporterWrapper => WrapperInfo.FindWrapper(this, ReporterSettings?.Wrapper);
        public bool WatchdogDisabled => _testingRunSettings?.WatchdogDisabled == true;
        public string WorkingDirectory => _testingRunSettings?.WorkingDirectory;
        public string PathExtension => _testingRunSettings?.PathExtension;
        public Dictionary<string, string> EnvironmentVariables =>
            _testingRunSettings?.EnvironmentVariables?
                .Where(x => !string.IsNullOrEmpty(x?.Name))
                .ToDictionary(x => x.Name, x => x.Value ?? string.Empty)
            ?? new Dictionary<string, string>();

        private RunSettings(XDocument runSettings)
        {
            _runSettings = runSettings;

            var adapterSettings = _runSettings?.Root?.Element(Constants.SettingsName);
            if (adapterSettings != null)
            {
                _testingRunSettings = TestingRunSettings.LoadFromXml(adapterSettings.CreateNavigator());
            }

            Debug.WriteLine($"SolutionDirectory: {SolutionDirectory ?? "UNKNOWN"}");
            Debug.WriteLine($"ResultsDirectory: {ResultsDirectory ?? "UNKNOWN"}");
        }

        public string ComposePathVariable()
        {
            var path = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrWhiteSpace(SolutionDirectory))
            {
                path = $"{SolutionDirectory};{path}";
            }

            path = $"{Environment.CurrentDirectory};{path}";

            if (!string.IsNullOrWhiteSpace(PathExtension))
            {
                path = $"{PathExtension};{path}";
            }

            return path;
        }

        public static RunSettings FromRunSettings(IRunSettings runSettings)
        {
            if (string.IsNullOrWhiteSpace(runSettings?.SettingsXml))
            {
                return null;
            }

            try
            {
                return new RunSettings(XDocument.Parse(runSettings.SettingsXml));
            }
            catch (XmlException e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}
