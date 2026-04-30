using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.TestAdapter.Dia;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    internal class TestingExternalDiscoverer : ITestingDiscoverer
    {
        private readonly RunSettings _settings;
        private readonly IMessageLogger _logger;
        private readonly TestingTestCaseBuilder _builder;
        private readonly WrapperInfo _wrapper;

        internal TestingExternalDiscoverer(RunSettings settings, IMessageLogger logger)
        {
            _settings = settings;
            _logger = logger;
            _builder = new TestingTestCaseBuilder(_settings);
            try
            {
                _wrapper = _settings.DiscovererWrapper;
            }
            catch (Exception e)
            {
                _logger?.SendMessage(TestMessageLevel.Error, $"Failed to resolve DiscovererWrapper:{Environment.NewLine}{e}");
                throw;
            }
        }

        public IEnumerable<TestCase> DiscoverTests(IEnumerable<string> executables)
        {
            return from executable in executables
                   from testCase in DiscoverTests(executable)
                   select testCase;
        }

        private string RunWrapperAndCaptureOutput(string executable)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = _wrapper.GetExecutable(),
                Arguments = _wrapper.GetArguments($"\"{executable}\""),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            processStartInfo.Environment["PATH"] = _settings.ComposePathVariable();
            if (_settings.SolutionDirectory != null)
            {
                processStartInfo.Environment["SOLUTION_DIRECTORY"] = _settings.SolutionDirectory;
            }

            using (var process = Process.Start(processStartInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
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

            string output;
            try
            {
                output = RunWrapperAndCaptureOutput(executable);
            }
            catch (Exception e)
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
