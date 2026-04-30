using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    internal class MesonTestDiscoverer : ITestingDiscoverer
    {
        private class MesonTestEntry
        {
            public List<string> Cmd { get; set; }
            public Dictionary<string, string> Env { get; set; }
            public string Name { get; set; }
            public string Protocol { get; set; }
            public List<string> Extra_Paths { get; set; }
        }

        private readonly List<MesonTestEntry> _entries;
        private readonly RunSettings _settings;
        private readonly IMessageLogger _logger;

        private MesonTestDiscoverer(List<MesonTestEntry> entries, RunSettings settings, IMessageLogger logger)
        {
            _entries = entries;
            _settings = settings;
            _logger = logger;
        }

        public static MesonTestDiscoverer Create(string introTestsFile, RunSettings settings, IMessageLogger logger)
        {
            List<MesonTestEntry> entries;
            try
            {
                var json = File.ReadAllText(introTestsFile);
                var serializer = new JavaScriptSerializer();
                entries = serializer.Deserialize<List<MesonTestEntry>>(json) ?? new List<MesonTestEntry>();
            }
            catch
            {
                return null;
            }
            if (entries.Count == 0)
            {
                return null;
            }

            return new MesonTestDiscoverer(entries, settings, logger);
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> executables)
        {
            return from executable in executables
                   from testCase in DiscoverTests(executable)
                   select testCase;
        }

        public IEnumerable<TestCase> DiscoverTests(string executable)
        {
            var normalizedExe = Path.GetFullPath(executable).ToLowerInvariant();
            var matchingEntries = _entries.Where(entry =>
                !string.IsNullOrWhiteSpace(entry.Name) &&
                entry.Cmd != null &&
                entry.Cmd.Count > 0 &&
                Path.GetFullPath(entry.Cmd[0]).ToLowerInvariant() == normalizedExe &&
                entry.Protocol == "exitcode" // Only support exitcode protocol
            );

            foreach (var entry in matchingEntries)
            {
                var cmd = string.Join(" ", entry.Cmd.Skip(1).Select(arg =>
                    arg.Contains(' ') ? $"\"{arg}\"" : arg
                ));

                var pathExtensions = string.Join(";", entry.Extra_Paths ?? new List<string> { });
                var envDict = new Dictionary<string, string>(entry.Env ?? new Dictionary<string, string>());
                if (!string.IsNullOrWhiteSpace(pathExtensions))
                {
                    if (envDict.TryGetValue("PATH", out var path))
                    {
                        envDict["PATH"] = pathExtensions + ";" + path;
                    }
                    else
                    {
                        envDict["PATH"] = pathExtensions;
                    }
                }
                var envVars = envDict.Select(kv => $"{kv.Key}={kv.Value}").ToArray();

                var test = new TestCase(entry.Name, Constants.ExecutorUri, executable)
                {
                    DisplayName = entry.Name,
                };

                test.SetPropertyValue(Property.TestArgument, cmd);
                test.SetPropertyValue(Property.TestEnvironment, envVars);

                yield return test;
            }
        }
    }
}
