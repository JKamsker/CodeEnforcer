using System.Diagnostics;

namespace CodeEnforcer;

internal static class CodeFileCollector
{
    public static CodebaseSnapshot Collect(string root)
    {
        List<CodeFile> files = [];
        string[] trackedPaths = ListTrackedFiles(root);
        foreach (string path in trackedPaths.Where(IsCSharpFile))
        {
            string normalizedPath = PathUtility.Normalize(path);
            if (ShouldSkip(normalizedPath))
            {
                continue;
            }

            string fullPath = Path.Combine(root, normalizedPath);
            files.Add(new CodeFile(normalizedPath, File.ReadLines(fullPath).Count()));
        }

        HashSet<string> projectFolders = trackedPaths
            .Where(IsProjectFile)
            .Select(path => PathUtility.GetDirectory(PathUtility.Normalize(path)))
            .ToHashSet(StringComparer.Ordinal);

        string[] normalizedTrackedPaths = trackedPaths
            .Select(PathUtility.Normalize)
            .Where(path => !ShouldSkip(path))
            .ToArray();

        return new CodebaseSnapshot(files, projectFolders, normalizedTrackedPaths);
    }

    internal static bool ShouldSkip(string path) =>
        path.Contains("/bin/", StringComparison.Ordinal) ||
        path.Contains("/obj/", StringComparison.Ordinal) ||
        path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);

    private static bool IsCSharpFile(string path) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    private static bool IsProjectFile(string path) =>
        path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);

    private static string[] ListTrackedFiles(string root)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(root);
        startInfo.ArgumentList.Add("ls-files");

        using Process process = Process.Start(startInfo)
            ?? throw new CodeEnforcerException("Failed to start git.", ExitCodes.InternalError);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new CodeEnforcerException("git ls-files failed: " + error.Trim(), ExitCodes.InputError);
        }

        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}
