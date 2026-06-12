using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeEnforcer;

internal abstract class JustificationCommandSettings : CommandSettings
{
    [CommandOption("--config <PATH>")]
    [Description("Config file path. Defaults to .config/code-enforcer/code-enforcer.json found from cwd parents.")]
    public string? ConfigPath { get; init; }
}

internal abstract class TypedJustificationCommandSettings : JustificationCommandSettings
{
    [CommandOption("--type <TYPE>")]
    [Description("Entry type: file, folder, or root-folder.")]
    public string? Type { get; init; }

    public JustificationEntryType EntryType => JustificationEntryTypes.Parse(Type ?? string.Empty);

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            return ValidationResult.Error("--type is required.");
        }

        try
        {
            _ = EntryType;
        }
        catch (CodeEnforcerException ex)
        {
            return ValidationResult.Error(ex.Message);
        }

        return ValidationResult.Success();
    }
}

internal abstract class PathJustificationCommandSettings : TypedJustificationCommandSettings
{
    [CommandOption("--path <PATH>")]
    [Description("Entry path or path pattern.")]
    public string? Path { get; init; }

    public override ValidationResult Validate()
    {
        ValidationResult result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        return string.IsNullOrWhiteSpace(Path)
            ? ValidationResult.Error("--path is required.")
            : ValidationResult.Success();
    }
}
