using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Phantom.Testing.Common
{
    public static class PathUtils
    {
        public static string FindExecutableInPath(string command)
        {
            var exts = Path.HasExtension(command) ? new[] { "" } : (Environment.GetEnvironmentVariable("PATHEXT") ?? ".exe;.bat;.cmd").Split(';');
            var paths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                foreach (var ext in exts)
                {
                    try
                    {
                        var commandPath = Path.GetFullPath(Path.Combine(path, command + ext));
                        if (File.Exists(commandPath))
                        {
                            return commandPath;
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        public static string FindExecutable(string command, IEnumerable<string> searchPaths)
        {
            if (!command.Contains(Path.DirectorySeparatorChar) && !command.Contains(Path.AltDirectorySeparatorChar))
            {
                var wrapperPath = FindExecutableInPath(command);
                if (!string.IsNullOrWhiteSpace(wrapperPath))
                {
                    return wrapperPath;
                }
            }

            foreach (var path in searchPaths)
            {
                try
                {
                    var commandPath = Path.GetFullPath(Path.Combine(path, command));
                    if (File.Exists(commandPath))
                    {
                        return commandPath;
                    }
                }
                catch { }
            }

            return null;
        }

        public static string FindFile(string file, IEnumerable<string> searchPaths)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            if (Path.IsPathRooted(file))
            {
                return File.Exists(file) ? file : null;
            }

            var fullPath = Path.GetFullPath(file);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            foreach (var path in searchPaths)
            {
                try
                {
                    fullPath = Path.GetFullPath(Path.Combine(path, file));
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch { }
            }

            return null;
        }

        public static string FindDirectory(string folder, IEnumerable<string> searchPaths)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                return null;
            }

            if (Path.IsPathRooted(folder))
            {
                return Directory.Exists(folder) ? folder : null;
            }

            var fullPath = Path.GetFullPath(folder);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            foreach (var path in searchPaths)
            {
                try
                {
                    fullPath = Path.GetFullPath(Path.Combine(path, folder));
                    if (Directory.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch { }
            }

            return null;
        }

        public static string FindPythonInPath()
        {
            var candidates = new[] { "py", "python", "python3" };
            foreach (var candidate in candidates)
            {
                var python = FindExecutableInPath(candidate);
                if (!string.IsNullOrWhiteSpace(python))
                {
                    return python;
                }
            }
            return "python";
        }
    }
}
