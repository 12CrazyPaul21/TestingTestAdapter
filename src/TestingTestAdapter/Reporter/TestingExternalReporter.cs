using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web.Script.Serialization;

namespace Phantom.Testing.TestAdapter.Reporter
{
    internal class TestingExternalReporter : TestingReporter
    {
        private readonly RunSettings _settings;
        private readonly WrapperInfo _wrapper;

        private TestingExternalReporter(TestCase test, IFrameworkHandle frameworkHandle) : base(test, frameworkHandle)
        {
        }

        public TestingExternalReporter(TestCase test, RunSettings settings, IFrameworkHandle frameworkHandle) :
            this(test, frameworkHandle)
        {
            _settings = settings;
            try
            {
                _wrapper = _settings.ReporterWrapper;
            }
            catch (Exception e)
            {
                frameworkHandle?.SendMessage(TestMessageLevel.Error, $"Failed to resolve ReporterWrapper:{Environment.NewLine}{e}");
                throw;
            }
        }

        private string RunWrapperAndCaptureReport(string json)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _wrapper.GetExecutable(),
                Arguments = _wrapper.GetArguments(string.Empty),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            };

            using (var process = Process.Start(processStartInfo))
            {
                process.StandardInput.Write(json);
                process.StandardInput.Close();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        protected override void PopulateResult(TestResult result)
        {
            var serializer = new JavaScriptSerializer();

            var request = serializer.Serialize(new WrapperRequest
            {
                TestName = _test.DisplayName,
                ExitCode = (int)_exitCode,
                StdOut = _stdout,
                StdErr = _stderr,
            });
            var response = serializer.Deserialize<WrapperReport>(
                RunWrapperAndCaptureReport(request)
            );
            if (response == null || !response.Verify())
            {
                throw new InvalidOperationException("Invalid wrapper response");
            }

            switch (response.Outcome)
            {
                case "Passed":
                    result.Outcome = TestOutcome.Passed;
                    break;
                case "Skipped":
                case "TODO":
                    result.Outcome = TestOutcome.Skipped;
                    result.Messages.Add(
                        new TestResultMessage(
                            TestResultMessage.StandardOutCategory,
                            response.Outcome
                        )
                    );
                    break;
                case "Failed":
                    result.Outcome = TestOutcome.Failed;
                    result.ErrorMessage = response.ErrorMessage ?? string.Empty;
                    result.ErrorStackTrace = response.ErrorStackTrace ?? string.Empty;
                    break;
            }
        }

        private class WrapperRequest
        {
            public string TestName { get; set; }
            public int ExitCode { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }
        }

        private class WrapperReport
        {
            private static readonly HashSet<string> ValidOutcomes = new HashSet<string>
            {
                "Passed",
                "Failed",
                "Skipped",
                "TODO"
            };

            public string Outcome { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorStackTrace { get; set; }

            public bool Verify()
            {
                if (Outcome == null)
                {
                    return false;
                }

                return ValidOutcomes.Contains(Outcome);
            }
        }
    }
}
