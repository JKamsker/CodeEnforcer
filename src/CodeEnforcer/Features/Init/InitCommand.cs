using Spectre.Console;
using Spectre.Console.Cli;

namespace CodeEnforcer;

internal sealed class InitCommand : Command<InitSettings>
{
    private readonly IAnsiConsole output;
    private readonly IAnsiConsole error;

    public InitCommand()
        : this(AnsiConsole.Console, ConsoleFactory.CreateErrorConsole())
    {
    }

    internal InitCommand(IAnsiConsole output, IAnsiConsole error)
    {
        this.output = output;
        this.error = error;
    }

    protected override int Execute(
        CommandContext context,
        InitSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            InitResult result = CodeEnforcerInitializer.Initialize(
                settings.RootDirectory ?? Environment.CurrentDirectory,
                settings.Force);
            WriteResult(result);
            return ExitCodes.Success;
        }
        catch (CodeEnforcerException ex)
        {
            WriteError(ex.Message);
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            WriteError(ex.Message);
            return ExitCodes.InternalError;
        }
    }

    private void WriteResult(InitResult result)
    {
        output.MarkupLine($"Repository: [green]{Markup.Escape(result.RepositoryRoot)}[/]");
        WriteFileStatus("Config", result.Config);
        WriteFileStatus("Justifications", result.Justifications);
        WriteFileStatus("Pre-commit hook", result.Hook);
        output.MarkupLine("[green]Configured git core.hooksPath to .githooks.[/]");
    }

    private void WriteFileStatus(string label, InitFileResult file)
    {
        string status = file.Written ? "wrote" : "kept";
        output.MarkupLine($"{label}: [green]{status}[/] {Markup.Escape(file.Path)}");
    }

    private void WriteError(string message) =>
        error.MarkupLine($"[red]error:[/] {Markup.Escape(message)}");
}
