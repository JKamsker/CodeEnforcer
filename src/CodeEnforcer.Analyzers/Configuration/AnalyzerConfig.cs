using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeEnforcer.Analyzers;

internal sealed class AnalyzerConfig
{
    private static readonly char[] ListSeparators = [';', ','];

    private const int DefaultSoftLineLimit = 350;
    private const int DefaultHardLineLimit = 500;
    private const int DefaultMaxFilesPerFolder = 15;

    private AnalyzerConfig(
        int softLineLimit,
        int hardLineLimit,
        int maxFilesPerFolder,
        int maxFilesInProjectFolder,
        string? projectDirectory,
        PathExclusionSet fileExclusions,
        PathExclusionSet hardFileJustifications,
        PathExclusionSet folderExclusions,
        PathExclusionSet projectFolderExclusions)
    {
        SoftLineLimit = softLineLimit;
        HardLineLimit = hardLineLimit;
        MaxFilesPerFolder = maxFilesPerFolder;
        MaxFilesInProjectFolder = maxFilesInProjectFolder;
        ProjectDirectory = projectDirectory;
        FileExclusions = fileExclusions;
        HardFileJustifications = hardFileJustifications;
        FolderExclusions = folderExclusions;
        ProjectFolderExclusions = projectFolderExclusions;
    }

    public int SoftLineLimit { get; }

    public int HardLineLimit { get; }

    public int MaxFilesPerFolder { get; }

    public int MaxFilesInProjectFolder { get; }

    public string? ProjectDirectory { get; }

    public PathExclusionSet FileExclusions { get; }

    public PathExclusionSet HardFileJustifications { get; }

    public PathExclusionSet FolderExclusions { get; }

    public PathExclusionSet ProjectFolderExclusions { get; }

    public static AnalyzerConfig From(
        AnalyzerConfigOptions globalOptions,
        AnalyzerConfigOptions? treeOptions = null)
    {
        int softLineLimit = ReadPositiveInt(
            treeOptions,
            globalOptions,
            "dotnet_code_enforcer_max_lines_soft",
            DefaultSoftLineLimit);
        int hardLineLimit = ReadPositiveInt(
            treeOptions,
            globalOptions,
            "dotnet_code_enforcer_max_lines_hard",
            DefaultHardLineLimit);
        int maxFilesPerFolder = ReadPositiveInt(
            treeOptions,
            globalOptions,
            "dotnet_code_enforcer_max_files_per_dir",
            DefaultMaxFilesPerFolder);
        int maxFilesInProjectFolder = ReadPositiveInt(
            treeOptions,
            globalOptions,
            "dotnet_code_enforcer_max_files_per_root_dir",
            maxFilesPerFolder);

        if (softLineLimit > hardLineLimit)
        {
            hardLineLimit = softLineLimit;
        }

        string? projectDirectory = ReadFirst(
            globalOptions,
            "build_property.ProjectDir",
            "build_property.MSBuildProjectDirectory");

        return new AnalyzerConfig(
            softLineLimit,
            hardLineLimit,
            maxFilesPerFolder,
            maxFilesInProjectFolder,
            projectDirectory,
            ReadPathSet(treeOptions, globalOptions, "dotnet_code_enforcer_file_exclusions"),
            ReadJustificationSet(treeOptions, globalOptions, "dotnet_code_enforcer_hard_file_justifications"),
            ReadPathSet(treeOptions, globalOptions, "dotnet_code_enforcer_folder_exclusions"),
            ReadPathSet(treeOptions, globalOptions, "dotnet_code_enforcer_root_folder_exclusions"));
    }

    private static int ReadPositiveInt(
        AnalyzerConfigOptions? treeOptions,
        AnalyzerConfigOptions options,
        string name,
        int defaultValue)
    {
        if (!TryGetValue(treeOptions, options, name, out string? value) ||
            !int.TryParse(value, out int parsed) ||
            parsed <= 0)
        {
            return defaultValue;
        }

        return parsed;
    }

    private static string? ReadFirst(AnalyzerConfigOptions options, params string[] names)
    {
        foreach (string name in names)
        {
            if (options.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static PathExclusionSet ReadPathSet(
        AnalyzerConfigOptions? treeOptions,
        AnalyzerConfigOptions options,
        string name)
    {
        if (!TryGetValue(treeOptions, options, name, out string? value) || value is null)
        {
            return PathExclusionSet.Empty;
        }

        return PathExclusionSet.FromPaths(SplitList(value));
    }

    private static PathExclusionSet ReadJustificationSet(
        AnalyzerConfigOptions? treeOptions,
        AnalyzerConfigOptions options,
        string name)
    {
        if (!TryGetValue(treeOptions, options, name, out string? value) || value is null)
        {
            return PathExclusionSet.Empty;
        }

        List<string> justifiedPaths = new();
        foreach (string entry in SplitList(value))
        {
            int separator = entry.IndexOf('=');
            if (separator <= 0 || separator == entry.Length - 1)
            {
                continue;
            }

            string path = entry.Substring(0, separator).Trim();
            string justification = entry.Substring(separator + 1).Trim();
            if (path.Length > 0 && justification.Length > 0)
            {
                justifiedPaths.Add(path);
            }
        }

        return PathExclusionSet.FromPaths(justifiedPaths);
    }

    private static IEnumerable<string> SplitList(string value)
    {
        string[] entries = value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
        foreach (string entry in entries)
        {
            string trimmed = entry.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }

    private static bool TryGetValue(
        AnalyzerConfigOptions? treeOptions,
        AnalyzerConfigOptions globalOptions,
        string name,
        out string? value)
    {
        if (treeOptions is not null && treeOptions.TryGetValue(name, out value))
        {
            return true;
        }

        return globalOptions.TryGetValue(name, out value);
    }
}
