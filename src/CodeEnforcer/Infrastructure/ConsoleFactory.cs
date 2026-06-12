using Spectre.Console;

namespace CodeEnforcer;

internal static class ConsoleFactory
{
    public static IAnsiConsole CreateErrorConsole() =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(Console.Error)
        });
}
