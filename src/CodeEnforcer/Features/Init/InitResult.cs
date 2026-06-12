namespace CodeEnforcer;

internal sealed class InitResult
{
    public InitResult(
        string repositoryRoot,
        InitFileResult config,
        InitFileResult justifications,
        InitFileResult hook)
    {
        RepositoryRoot = repositoryRoot;
        Config = config;
        Justifications = justifications;
        Hook = hook;
    }

    public string RepositoryRoot { get; }

    public InitFileResult Config { get; }

    public InitFileResult Justifications { get; }

    public InitFileResult Hook { get; }
}

internal sealed class InitFileResult
{
    public InitFileResult(string path, bool written)
    {
        Path = path;
        Written = written;
    }

    public string Path { get; }

    public bool Written { get; }
}
