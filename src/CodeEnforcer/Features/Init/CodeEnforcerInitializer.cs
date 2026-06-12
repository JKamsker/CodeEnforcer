using System.Diagnostics;
using System.Text;

namespace CodeEnforcer;

internal static class CodeEnforcerInitializer
{
    private const string HookPath = ".githooks";

    public static InitResult Initialize(string startDirectory, bool force)
    {
        string repositoryRoot = RepositoryPaths.DiscoverRoot(startDirectory);
        string configDirectory = Path.Combine(repositoryRoot, ".config", "code-enforcer");
        Directory.CreateDirectory(configDirectory);

        InitFileResult config = WriteFile(
            Path.Combine(configDirectory, "code-enforcer.json"),
            DefaultConfigJson(),
            force);
        InitFileResult justifications = WriteFile(
            Path.Combine(configDirectory, "justifications.json"),
            DefaultJustificationsJson(),
            force);

        string hookDirectory = Path.Combine(repositoryRoot, HookPath);
        Directory.CreateDirectory(hookDirectory);
        InitFileResult hook = WriteFile(
            Path.Combine(hookDirectory, "pre-commit"),
            PreCommitHook(),
            force);
        MakeExecutableOnUnix(hook.Path);
        ConfigureHooksPath(repositoryRoot);

        return new InitResult(repositoryRoot, config, justifications, hook);
    }

    private static InitFileResult WriteFile(string path, string content, bool force)
    {
        if (File.Exists(path) && !force)
        {
            return new InitFileResult(path, written: false);
        }

        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return new InitFileResult(path, written: true);
    }

    private static string DefaultConfigJson() =>
        """
        {
          "version": 1,
          "maxFilesPerDir": 15,
          "maxFilesPerRootDir": 5,
          "maxLinesSoft": 300,
          "maxLinesHard": 500
        }
        """;

    private static string DefaultJustificationsJson() =>
        """
        {
          "version": 1,
          "files": [],
          "folders": [],
          "rootFolders": []
        }
        """;

    private static string PreCommitHook() =>
        """
        #!/usr/bin/env sh
        set -eu

        if command -v code-enforcer >/dev/null 2>&1; then
          code-enforcer check
        else
          dotnet tool run code-enforcer -- check
        fi
        """;

    private static void ConfigureHooksPath(string repositoryRoot)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(repositoryRoot);
        startInfo.ArgumentList.Add("config");
        startInfo.ArgumentList.Add("core.hooksPath");
        startInfo.ArgumentList.Add(HookPath);

        using Process process = Process.Start(startInfo)
            ?? throw new CodeEnforcerException("Failed to start git.", ExitCodes.InternalError);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new CodeEnforcerException("git config failed: " + error.Trim(), ExitCodes.InputError);
        }
    }

    private static void MakeExecutableOnUnix(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }
}
