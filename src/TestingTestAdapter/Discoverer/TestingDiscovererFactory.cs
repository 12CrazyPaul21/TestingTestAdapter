using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Phantom.Testing.Common;
using Phantom.Testing.TestAdapter.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Phantom.Testing.TestAdapter.Discoverer
{
    internal static class TestingDiscovererFactory
    {
        internal static ITestingDiscoverer Create(RunSettings settings, IMessageLogger logger)
        {
            if (settings?.DiscovererUseWrapper == true)
            {
                return new TestingExternalDiscoverer(settings, logger);
            }
            else
            {
                return new TestingDiscoverer(settings, logger);
            }
        }

        internal static ITestingDiscoverer CreateFallback(string callerDiscovererName, RunSettings settings, IMessageLogger logger)
        {
            if (callerDiscovererName == nameof(TestingDiscoverer))
            {
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
                var mesonInfoDir = PathUtils.FindDirectory("meson-info", searchPaths);
                if (Directory.Exists(mesonInfoDir))
                {
                    var introTestsFile = Path.Combine(mesonInfoDir, "intro-tests.json");
                    if (File.Exists(introTestsFile))
                    {
                        return MesonTestDiscoverer.Create(introTestsFile, settings, logger);
                    }
                }
            }
            return null;
        }
    }
}
