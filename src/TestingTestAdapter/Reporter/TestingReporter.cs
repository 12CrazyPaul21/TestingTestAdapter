using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Linq;

namespace Phantom.Testing.TestAdapter.Reporter
{
    internal class TestingReporter : ITestingReporter
    {
        protected readonly TestCase _test;
        protected readonly IFrameworkHandle _frameworkHandle;

        protected Exception _exception;
        protected int? _exitCode;
        protected string _stdout;
        protected string _stderr;

        public TestingReporter(TestCase test, IFrameworkHandle frameworkHandle)
        {
            _test = test;
            _frameworkHandle = frameworkHandle;
        }

        public void RecordStart()
        {
            _frameworkHandle.RecordStart(_test);
        }

        protected virtual void PopulateResult(TestResult result)
        {
            if (_exitCode == 0)
            {
                var testLine = _stdout
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => (x.StartsWith("ok") || x.StartsWith("not ok")) && x.Contains(_test.DisplayName))
                    .FirstOrDefault();
                if (testLine != null && (testLine.Contains("# SKIP") || testLine.Contains("# TODO")))
                {
                    result.Outcome = TestOutcome.Skipped;
                    result.Messages.Add(
                        new TestResultMessage(
                            TestResultMessage.StandardOutCategory,
                            testLine.Contains("# SKIP") ? "Skipped" : "TODO"
                        )
                    );
                }
                else
                {
                    result.Outcome = TestOutcome.Passed;
                }
            }
            else
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = _stderr;
            }
        }

        public void RecordEnd()
        {
            var result = new TestResult(_test);

            if (_exception != null)
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = _exception.ToString();
                result.ErrorStackTrace = _exception.StackTrace;
            }
            else if (_exitCode == null)
            {
                throw new InvalidOperationException("Test result was not recorded");
            }
            else
            {
                PopulateResult(result);

                if (_test.Traits.Any(t => t.Name == "expected" && t.Value == "failure"))
                {
                    if (result.Outcome == TestOutcome.Failed)
                    {
                        result.Outcome = TestOutcome.Passed;
                        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                        {
                            var errorMessage = result.ErrorMessage;
                            result.Messages.Add(
                                new TestResultMessage(TestResultMessage.StandardErrorCategory, $"[xfail error]\n{errorMessage}\n")
                            );
                            result.ErrorMessage = null;
                        }
                        if (!string.IsNullOrWhiteSpace(result.ErrorStackTrace))
                        {
                            var errorStackTrace = result.ErrorStackTrace;
                            result.Messages.Add(
                                new TestResultMessage(TestResultMessage.StandardErrorCategory, $"[xfail stacktrace]\n{errorStackTrace}\n")
                            );
                            result.ErrorStackTrace = null;
                        }
                    }
                    else if (result.Outcome == TestOutcome.Passed)
                    {
                        result.Outcome = TestOutcome.Failed;
                        result.ErrorMessage = "Unexpected pass (expected failure)";
                    }
                }
            }

            _frameworkHandle.RecordResult(result);
            _frameworkHandle.RecordEnd(_test, result.Outcome);
        }

        public void RecordResult(int exitCode, string stdout, string stderr)
        {
            _exitCode = exitCode;
            _stdout = stdout ?? string.Empty;
            _stderr = stderr ?? string.Empty;
        }

        public void RecordException(Exception exception)
        {
            _exception = exception;
        }
    }
}
