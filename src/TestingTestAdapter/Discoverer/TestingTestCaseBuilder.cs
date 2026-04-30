using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Phantom.Testing.Common;
using Phantom.Testing.TestAdapter.Dia;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    using TestingTestDetails = Dictionary<string, Dictionary<string, List<string>>>;

    internal class TestingTestCaseBuilder
    {
        private readonly RunSettings _settings;

        public TestingTestCaseBuilder(RunSettings settings)
        {
            _settings = settings;
        }

        public TestingTestDetails DeserializeTestDetails(string rawJson)
        {
            var parsed = new Dictionary<string, List<Dictionary<string, string>>>();
            try
            {
                var serializer = new JavaScriptSerializer();
                parsed = serializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(rawJson) ?? parsed;
            }
            catch
            {
                return new TestingTestDetails();
            }

            return parsed.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .SelectMany(meta => meta)
                    .GroupBy(pair => pair.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(pair => pair.Value).ToList()
                    )
            );
        }

        public IEnumerable<TestCase> CreateTestCases(DiaResolver diaResolver, string executable, TestingTestDetails details)
        {
            foreach (var kv in details)
            {
                var name = kv.Key;
                var meta = kv.Value;

                meta.TryGetFirstValue("arg", out var arg);
                meta.TryGetFirstValue("suite", out var suite);
                meta.TryGetFirstValue("category", out var category);
                meta.TryGetValue("env", out var envs);

                var funcInfo = diaResolver?.GetFunctionInfo($"test_{name}");
                var file = funcInfo?.SourceFile;
                var lineNumber = funcInfo != null ? (int)funcInfo.LineNumber : 0;

                if (string.IsNullOrWhiteSpace(file))
                {
                    if (meta.TryGetFirstValue("file", out string fileFromDetail))
                    {
                        var searchPaths = new List<string>()
                        {
                            _settings.SolutionDirectory,
                            Path.GetDirectoryName(_settings.ResultsDirectory),
                            Environment.CurrentDirectory,
                            AppContext.BaseDirectory,
                        }
                        .Where(path => !string.IsNullOrWhiteSpace(path))
                        .Select(p => Path.GetFullPath(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
                        .Distinct();
                        file = PathUtils.FindFile(fileFromDetail, searchPaths);
                    }
                }
                if (lineNumber == 0 && !string.IsNullOrWhiteSpace(file))
                {
                    meta.TryGetFirstIntValue("line", out lineNumber);
                }

                string qualifiedName = "";
                if (!string.IsNullOrWhiteSpace(suite))
                {
                    qualifiedName += $"{suite}::";
                }
                if (!string.IsNullOrWhiteSpace(category))
                {
                    qualifiedName += $"{category}::";
                }
                qualifiedName += name;

                var test = new TestCase(qualifiedName, Constants.ExecutorUri, executable)
                {
                    DisplayName = name,
                    CodeFilePath = file,
                    LineNumber = lineNumber
                };

                test.SetPropertyValue(Property.TestArgument, arg ?? string.Empty);
                test.SetPropertyValue(Property.TestEnvironment, envs?.ToArray() ?? new string[] { });

                if (meta.TryGetValue("xfail", out var xfail) && xfail.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1"))
                {
                    test.Traits.Add(new Trait("expected", "failure"));
                }

                yield return test;
            }
        }
    }

    internal static class TestDetailExtensions
    {
        public static bool TryGetFirstValue(this Dictionary<string, List<string>> meta, string key, out string result)
        {
            if (!meta.TryGetValue(key, out var values))
            {
                result = null;
                return false;
            }

            if (values.Count == 0)
            {
                result = null;
                return false;
            }

            result = values[0];
            return true;
        }

        public static bool TryGetFirstIntValue(this Dictionary<string, List<string>> meta, string key, out int result)
        {
            if (!meta.TryGetFirstValue(key, out var value))
            {
                result = 0;
                return false;
            }

            return int.TryParse(value, out result);
        }
    }
}
