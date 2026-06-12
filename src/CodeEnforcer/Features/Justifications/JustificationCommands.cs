using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeEnforcer;

internal abstract class JustificationCommand<TSettings> : Command<TSettings>
    where TSettings : CommandSettings
{
    protected JustificationCommand()
        : this(AnsiConsole.Console, ConsoleFactory.CreateErrorConsole())
    {
    }

    protected JustificationCommand(IAnsiConsole output, IAnsiConsole error)
    {
        Output = output;
        Error = error;
    }

    protected IAnsiConsole Output { get; }

    protected IAnsiConsole Error { get; }

    protected sealed override int Execute(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            return ExecuteJustification(context, settings, cancellationToken);
        }
        catch (CodeEnforcerException ex)
        {
            Error.MarkupLine($"[red]error:[/] {Markup.Escape(ex.Message)}");
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Error.MarkupLine($"[red]error:[/] {Markup.Escape(ex.Message)}");
            return ExitCodes.InternalError;
        }
    }

    protected abstract int ExecuteJustification(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellationToken);
}

internal sealed class ListJustificationsCommand : JustificationCommand<ListJustificationsSettings>
{
    protected override int ExecuteJustification(
        CommandContext context,
        ListJustificationsSettings settings,
        CancellationToken cancellationToken)
    {
        JustificationStore store = JustificationStore.Open(settings.ConfigPath);
        JustificationEntryType[] types = settings.EntryType is { } requestedType
            ? [requestedType]
            : [JustificationEntryType.File, JustificationEntryType.Folder, JustificationEntryType.RootFolder];

        foreach (JustificationEntryType entryType in types)
        {
            WriteEntries(entryType, store.GetEntries(entryType));
        }

        return ExitCodes.Success;
    }

    private void WriteEntries(JustificationEntryType type, IReadOnlyList<PathExclusion> entries)
    {
        Output.MarkupLine($"[bold]{Markup.Escape(JustificationEntryTypes.Format(type))}[/]");
        if (entries.Count == 0)
        {
            Output.MarkupLine("  [grey]none[/]");
            return;
        }

        foreach (PathExclusion entry in entries.OrderBy(entry => entry.Path, StringComparer.Ordinal))
        {
            WriteEntry(entry, indent: true);
        }
    }

    private void WriteEntry(PathExclusion entry, bool indent)
    {
        string prefix = indent ? "  " : string.Empty;
        Output.MarkupLine($"{prefix}[green]{Markup.Escape(entry.Path)}[/]");
        if (!string.IsNullOrWhiteSpace(entry.Justification))
        {
            Output.MarkupLine($"{prefix}  {Markup.Escape(entry.Justification)}");
        }
    }
}

internal sealed class ShowJustificationCommand : JustificationCommand<ShowJustificationSettings>
{
    protected override int ExecuteJustification(
        CommandContext context,
        ShowJustificationSettings settings,
        CancellationToken cancellationToken)
    {
        JustificationStore store = JustificationStore.Open(settings.ConfigPath);
        PathExclusion entry = store.Find(settings.EntryType, settings.Path!) ??
            throw new CodeEnforcerException("Justification entry was not found.", ExitCodes.InputError);

        Output.MarkupLine($"Type: [green]{Markup.Escape(JustificationEntryTypes.Format(settings.EntryType))}[/]");
        Output.MarkupLine($"Path: [green]{Markup.Escape(entry.Path)}[/]");
        Output.MarkupLine($"Justification: {Markup.Escape(entry.Justification ?? string.Empty)}");
        return ExitCodes.Success;
    }
}

internal sealed class AddJustificationCommand : JustificationCommand<AddJustificationSettings>
{
    protected override int ExecuteJustification(
        CommandContext context,
        AddJustificationSettings settings,
        CancellationToken cancellationToken)
    {
        JustificationStore store = JustificationStore.Open(settings.ConfigPath);
        PathExclusion entry = store.Add(settings.EntryType, settings.Path!, settings.Justification);
        store.Save();
        Output.MarkupLine(
            $"Added [green]{Markup.Escape(JustificationEntryTypes.Format(settings.EntryType))}[/] justification for [green]{Markup.Escape(entry.Path)}[/].");
        return ExitCodes.Success;
    }
}

internal sealed class UpdateJustificationCommand : JustificationCommand<UpdateJustificationSettings>
{
    protected override int ExecuteJustification(
        CommandContext context,
        UpdateJustificationSettings settings,
        CancellationToken cancellationToken)
    {
        JustificationStore store = JustificationStore.Open(settings.ConfigPath);
        PathExclusion entry = store.Update(
            settings.EntryType,
            settings.Path!,
            settings.NewPath,
            settings.Justification,
            settings.ClearJustification);
        store.Save();
        Output.MarkupLine(
            $"Updated [green]{Markup.Escape(JustificationEntryTypes.Format(settings.EntryType))}[/] justification for [green]{Markup.Escape(entry.Path)}[/].");
        return ExitCodes.Success;
    }
}

internal sealed class RemoveJustificationCommand : JustificationCommand<RemoveJustificationSettings>
{
    protected override int ExecuteJustification(
        CommandContext context,
        RemoveJustificationSettings settings,
        CancellationToken cancellationToken)
    {
        JustificationStore store = JustificationStore.Open(settings.ConfigPath);
        PathExclusion entry = store.Remove(settings.EntryType, settings.Path!);
        store.Save();
        Output.MarkupLine(
            $"Removed [green]{Markup.Escape(JustificationEntryTypes.Format(settings.EntryType))}[/] justification for [green]{Markup.Escape(entry.Path)}[/].");
        return ExitCodes.Success;
    }
}
