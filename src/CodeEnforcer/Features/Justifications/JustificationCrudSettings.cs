using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeEnforcer;

internal sealed class ListJustificationsSettings : JustificationCommandSettings
{
    [CommandOption("--type <TYPE>")]
    [Description("Optional entry type filter: file, folder, or root-folder.")]
    public string? Type { get; init; }

    public JustificationEntryType? EntryType =>
        string.IsNullOrWhiteSpace(Type) ? null : JustificationEntryTypes.Parse(Type);

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            return ValidationResult.Success();
        }

        try
        {
            _ = EntryType;
            return ValidationResult.Success();
        }
        catch (CodeEnforcerException ex)
        {
            return ValidationResult.Error(ex.Message);
        }
    }
}

internal sealed class ShowJustificationSettings : PathJustificationCommandSettings;

internal sealed class AddJustificationSettings : PathJustificationCommandSettings
{
    [CommandOption("--justification <TEXT>")]
    [Description("Optional justification text. Required by checks only when a file exceeds the hard line limit.")]
    public string? Justification { get; init; }
}

internal sealed class UpdateJustificationSettings : PathJustificationCommandSettings
{
    [CommandOption("--new-path <PATH>")]
    [Description("Replacement path or path pattern.")]
    public string? NewPath { get; init; }

    [CommandOption("--justification <TEXT>")]
    [Description("Replacement justification text.")]
    public string? Justification { get; init; }

    [CommandOption("--clear-justification")]
    [Description("Remove the current justification text.")]
    public bool ClearJustification { get; init; }

    public override ValidationResult Validate()
    {
        ValidationResult result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        if (ClearJustification && Justification is not null)
        {
            return ValidationResult.Error("--clear-justification cannot be combined with --justification.");
        }

        return string.IsNullOrWhiteSpace(NewPath) && Justification is null && !ClearJustification
            ? ValidationResult.Error("At least one of --new-path, --justification, or --clear-justification is required.")
            : ValidationResult.Success();
    }
}

internal sealed class RemoveJustificationSettings : PathJustificationCommandSettings;
