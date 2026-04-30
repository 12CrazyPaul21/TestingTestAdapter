using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.TestAdapter.Dia;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    public class TestingDiscoverer : ITestingDiscoverer
    {
        public const string TestingIndicator = "testing_adapter_indicator_v1";
        public const string TestingDiscoverCmd = "--list-details";

        private readonly RunSettings _settings;
        private readonly IMessageLogger _logger;
        private readonly TestingTestCaseBuilder _builder;
        private readonly ITestingDiscoverer _fallback;

        internal TestingDiscoverer(RunSettings settings, IMessageLogger logger)
        {
            _settings = settings;
            _logger = logger;
            _builder = new TestingTestCaseBuilder(_settings);
            _fallback = TestingDiscovererFactory.CreateFallback(nameof(TestingDiscoverer), settings, logger);
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> executables)
        {
            return from executable in executables
                   from testCase in DiscoverTests(executable)
                   select testCase;
        }

        public IEnumerable<TestCase> DiscoverTests(string executable)
        {
            DiaResolver diaResolver = null;
            try
            {
                diaResolver = new DiaResolver(Path.ChangeExtension(executable, "pdb"));
            }
            catch (Exception e)
            {
                _logger?.SendMessage(
                    TestMessageLevel.Warning,
                    $"Failed to load PDB for '{executable}': {e.Message}"
                );
            }

            if (diaResolver == null || !diaResolver.HasFunction(TestingIndicator))
            {
                foreach (var test in _fallback.DiscoverTests(executable))
                {
                    yield return test;
                }
                yield break;
            }

            string output = string.Empty;
            try
            {
                var processStartInfo = new ProcessStartInfo(executable, TestingDiscoverCmd)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                processStartInfo.Environment["PATH"] = _settings.ComposePathVariable();

                using (var process = Process.Start(processStartInfo))
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                _logger?.SendMessage(TestMessageLevel.Error, e.ToString());
                yield break;
            }

            foreach (var test in _builder.CreateTestCases(diaResolver, executable, _builder.DeserializeTestDetails(output)))
            {
                yield return test;
            }
        }
    }
}
