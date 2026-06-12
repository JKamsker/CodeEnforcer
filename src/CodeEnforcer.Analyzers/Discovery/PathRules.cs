using System;
using System.Text.RegularExpressions;

namespace CodeEnforcer.Analyzers;

internal static class PathRules
{
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path!.Replace('\\', '/').Trim().TrimStart('.', '/');
    }

    public static string Folder(string path)
    {
        string normalized = Normalize(path);
        int lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? "." : normalized.Substring(0, lastSlash);
    }

    public static bool IsGeneratedOrBuildOutput(string path)
    {
        string normalized = Normalize(path);
        return normalized.IndexOf("/bin/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) >= 0 ||
            normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMatch(string path, string pattern)
    {
        string normalizedPath = Normalize(path);
        string normalizedPattern = Normalize(pattern);
        if (normalizedPattern.IndexOf('*') < 0)
        {
            return string.Equals(normalizedPath, normalizedPattern, StringComparison.Ordinal);
        }

        string regex = "^" + Regex.Escape(normalizedPattern)
            .Replace("\\*\\*", ".*")
            .Replace("\\*", "[^/]*") + "$";
        return Regex.IsMatch(normalizedPath, regex, RegexOptions.CultureInvariant);
    }

    public static string ToProjectRelativePath(string filePath, string? projectDirectory)
    {
        string normalizedFilePath = Normalize(filePath);
        string normalizedProjectDirectory = Normalize(projectDirectory);
        if (normalizedProjectDirectory.Length == 0)
        {
            return normalizedFilePath;
        }

        string projectPrefix = normalizedProjectDirectory.EndsWith("/", StringComparison.Ordinal)
            ? normalizedProjectDirectory
            : normalizedProjectDirectory + "/";

        return normalizedFilePath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase)
            ? normalizedFilePath.Substring(projectPrefix.Length)
            : normalizedFilePath;
    }
}
