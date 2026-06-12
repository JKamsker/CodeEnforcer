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
    AddJustificationCommands(configuration, "justifications");
    AddJustificationCommands(configuration, "exceptions");
});
return app.Run(NormalizeArgs(args));

static void AddJustificationCommands(IConfigurator configuration, string branchName)
{
    configuration.AddBranch(branchName, branch =>
    {
        branch.SetDescription("Manages CodeEnforcer justifications and exceptions.");
        branch.AddCommand<ListJustificationsCommand>("list")
            .WithDescription("Lists justification entries.");
        branch.AddCommand<ShowJustificationCommand>("show")
            .WithDescription("Shows one justification entry.");
        branch.AddCommand<AddJustificationCommand>("add")
            .WithDescription("Adds a justification entry.");
        branch.AddCommand<UpdateJustificationCommand>("update")
            .WithDescription("Updates a justification entry.");
        branch.AddCommand<RemoveJustificationCommand>("remove")
            .WithDescription("Removes a justification entry.");
        branch.AddCommand<RemoveJustificationCommand>("delete")
            .WithDescription("Deletes a justification entry.");
    });
}

static string[] NormalizeArgs(string[] args)
{
    if (args.Length == 0 || args[0].StartsWith('-'))
    {
        return ["check", .. args];
    }

    return args;
}
