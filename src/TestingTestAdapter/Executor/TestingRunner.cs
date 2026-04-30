using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Phantom.Testing.TestAdapter.ProcessExecution;
using Phantom.Testing.TestAdapter.Reporter;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Phantom.Testing.TestAdapter.Executor
{
    internal static class TestingRunner
    {
        public static ITestingExecutor CreateExecutor(RunSettings settings, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (runContext.IsBeingDebugged)
            {
                return new TestingDebugger(settings, runContext, frameworkHandle);

            }
            else
            {
                return new TestingExecutor(settings, runContext, frameworkHandle);
            }
        }

        public static string GetTestParameters(TestCase test)
        {
            return test.GetPropertyValue(Property.TestArgument) as string ?? test.DisplayName;
        }

        public static string GetWorkingDirectory(RunSettings settings, TestCase test)
        {
            return settings?.WorkingDirectory ?? Path.GetDirectoryName(test.Source);
        }

        public static Dictionary<string, string> GetEnvironments(RunSettings settings, TestCase test)
        {
            var variables = new Dictionary<string, string>(settings.EnvironmentVariables);

            if (test.GetPropertyValue(Property.TestEnvironment) is string[] testEnvironment)
            {
                var testVariables = testEnvironment.Select(x =>
                {
                    var idx = x.IndexOf('=');
                    if (idx < 0)
                    {
                        return null;
                    }
                    else
                    {
                        return new
                        {
                            Key = x.Substring(0, idx),
                            Value = x.Substring(idx + 1)
                        };
                    }
                }).Where(x => x != null);
                foreach (var variable in testVariables)
                {
                    variables[variable.Key] = variable.Value;
                }
            }

            if (!variables.TryGetValue("PATH", out var path))
            {
                path = Environment.GetEnvironmentVariable("PATH");
            }

            if (!string.IsNullOrWhiteSpace(settings.SolutionDirectory))
            {
                path = $"{settings.SolutionDirectory};{path}";
            }

            path = $"{Environment.CurrentDirectory};{path}";

            if (!string.IsNullOrWhiteSpace(settings.PathExtension))
            {
                path = $"{settings.PathExtension};{path}";
            }

            variables["PATH"] = path;
            return variables;
        }

        public static (IProcess, ITestingReporter) RunTest(TestCase test, IProcessLauncher launcher, RunSettings settings, IFrameworkHandle frameworkHandle)
        {
            IProcess process;
            ITestingReporter reporter = TestingReporterFactory.Create(test, settings, frameworkHandle);

            var parameters = GetTestParameters(test);
            var WorkingDirectory = GetWorkingDirectory(settings, test);
            var environments = GetEnvironments(settings, test);

            reporter.RecordStart();
            try
            {
                process = launcher.Spawn(test.Source, parameters, WorkingDirectory, environments);
            }
            catch (Exception e)
            {
                reporter.RecordException(e);
                reporter.RecordEnd();
                throw;
            }

            return (process, reporter);
        }

        public static void WaitAndReportTestExecution(IProcess process, ITestingReporter reporter)
        {
            try
            {
                process.WaitForExit();
                reporter.RecordResult(process.ExitCode, process.StdOut, process.StdErr);
            }
            catch (Exception e)
            {
                reporter.RecordException(e);
            }
            finally
            {
                reporter.RecordEnd();
            }
        }
    }
}
