using System.ComponentModel;
using Spectre.Console.Cli;

namespace CodeEnforcer;

internal sealed class InitSettings : CommandSettings
{
    [CommandOption("--root <PATH>")]
    [Description("Repository root to initialize. Defaults to the current repository root.")]
    public string? RootDirectory { get; init; }

    [CommandOption("--force")]
    [Description("Overwrite existing CodeEnforcer config and hook files.")]
    public bool Force { get; init; }
}
