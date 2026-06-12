using System.Diagnostics;
using CodeEnforcer;

namespace CodeEnforcer.Tests;

public sealed class CodeEnforcerInitializerTests : IDisposable
{
    private readonly string root;

    public CodeEnforcerInitializerTests()
    {
        root = Path.Combine(Path.GetTempPath(), "code-enforcer-init-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        RunGit("init");
    }

    [Fact]
    public void CreatesConfigFilesAndInstallsHook()
    {
        InitResult result = CodeEnforcerInitializer.Initialize(root, force: false);

        Assert.True(result.Config.Written);
        Assert.True(result.Justifications.Written);
        Assert.True(result.Hook.Written);
        Assert.True(File.Exists(Path.Combine(root, ".config", "code-enforcer", "code-enforcer.json")));
        Assert.True(File.Exists(Path.Combine(root, ".config", "code-enforcer", "justifications.json")));
        Assert.Contains("code-enforcer check", File.ReadAllText(Path.Combine(root, ".githooks", "pre-commit")));
        Assert.Equal(".githooks", RunGit("config", "core.hooksPath"));
    }

    [Fact]
    public void KeepsExistingFilesUnlessForced()
    {
        string configPath = Path.Combine(root, ".config", "code-enforcer", "code-enforcer.json");
        CodeEnforcerInitializer.Initialize(root, force: false);
        File.WriteAllText(configPath, "custom");

        InitResult result = CodeEnforcerInitializer.Initialize(root, force: false);

        Assert.False(result.Config.Written);
        Assert.Equal("custom", File.ReadAllText(configPath));
    }

    [Fact]
    public void ForceOverwritesExistingFiles()
    {
        string configPath = Path.Combine(root, ".config", "code-enforcer", "code-enforcer.json");
        CodeEnforcerInitializer.Initialize(root, force: false);
        File.WriteAllText(configPath, "custom");

        InitResult result = CodeEnforcerInitializer.Initialize(root, force: true);

        Assert.True(result.Config.Written);
        Assert.Contains("\"maxLinesSoft\": 300", File.ReadAllText(configPath));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string RunGit(params string[] arguments)
    {
        ProcessStartInfo startInfo = new("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = root
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error);
        }

        return output.Trim();
    }
}
