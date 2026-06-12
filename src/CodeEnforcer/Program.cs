using CodeEnforcer;
using Spectre.Console.Cli;

CommandApp app = new();
app.Configure(configuration =>
{
    configuration.SetApplicationName("CodeEnforcer");
    configuration.AddCommand<CheckCommand>("check")
        .WithDescription("Checks the repository against configured CodeEnforcer rules.");
    configuration.AddCommand<InitCommand>("init")
        .WithDescription("Initializes CodeEnforcer config files and the git pre-commit hook.");
});
return app.Run(NormalizeArgs(args));

static string[] NormalizeArgs(string[] args)
{
    if (args.Length == 0 || args[0].StartsWith('-'))
    {
        return ["check", .. args];
    }

    return args;
}
