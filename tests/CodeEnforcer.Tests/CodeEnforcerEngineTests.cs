using CodeEnforcer;

namespace CodeEnforcer.Tests;

public sealed class CodeEnforcerEngineTests
{
    [Fact]
    public void ReportsFileAboveSoftLimitWithoutExclusion()
    {
        CodeEnforcerConfig config = new() { SoftLineLimit = 350, HardLineLimit = 500 };

        IReadOnlyList<CodeViolation> violations = Check([new CodeFile("src/App/Large.cs", 351)], config);

        CodeViolation violation = Assert.Single(violations);
        Assert.Equal("CE0001", violation.Rule);
        Assert.Contains("soft limit", violation.Message);
    }

    [Fact]
    public void AllowsFileAboveSoftLimitWhenExcluded()
    {
        CodeEnforcerConfig config = new() { SoftLineLimit = 350, HardLineLimit = 500 };
        config.FileExclusions.Add(new PathExclusion { Path = "src/App/Large.cs" });

        IReadOnlyList<CodeViolation> violations = Check([new CodeFile("src/App/Large.cs", 351)], config);

        Assert.Empty(violations);
    }

    [Fact]
    public void RequiresJustificationAboveHardLimit()
    {
        CodeEnforcerConfig config = new() { SoftLineLimit = 350, HardLineLimit = 500 };
        config.FileExclusions.Add(new PathExclusion { Path = "src/App/Giant.cs" });

        IReadOnlyList<CodeViolation> violations = Check([new CodeFile("src/App/Giant.cs", 501)], config);

        CodeViolation violation = Assert.Single(violations);
        Assert.Equal("CE0002", violation.Rule);
        Assert.Contains("justification", violation.Message);
    }

    [Fact]
    public void AllowsHardLimitWhenExcludedWithJustification()
    {
        CodeEnforcerConfig config = new() { SoftLineLimit = 350, HardLineLimit = 500 };
        config.FileExclusions.Add(new PathExclusion
        {
            Path = "src/App/Giant.cs",
            Justification = "Legacy file awaiting split."
        });

        IReadOnlyList<CodeViolation> violations = Check([new CodeFile("src/App/Giant.cs", 501)], config);

        Assert.Empty(violations);
    }

    [Fact]
    public void ReportsFolderAboveFileLimit()
    {
        CodeEnforcerConfig config = new() { MaxFilesPerFolder = 2 };
        CodeFile[] files =
        [
            new("src/App/A.cs", 10),
            new("src/App/B.cs", 10),
            new("src/App/C.cs", 10)
        ];

        IReadOnlyList<CodeViolation> violations = Check(files, config);

        CodeViolation violation = Assert.Single(violations);
        Assert.Equal("CE0003", violation.Rule);
        Assert.Equal("src/App", violation.Path);
    }

    [Fact]
    public void AllowsFolderAboveFileLimitWhenExcluded()
    {
        CodeEnforcerConfig config = new() { MaxFilesPerFolder = 2 };
        config.FolderExclusions.Add(new PathExclusion { Path = "src/App" });

        IReadOnlyList<CodeViolation> violations = Check(
            [
                new CodeFile("src/App/A.cs", 10),
                new CodeFile("src/App/B.cs", 10),
                new CodeFile("src/App/C.cs", 10)
            ],
            config);

        Assert.Empty(violations);
    }

    [Fact]
    public void ReportsProjectFolderAboveFileLimit()
    {
        CodeEnforcerConfig config = new() { MaxFilesInProjectFolder = 2 };
        CodeFile[] files =
        [
            new("src/App/A.cs", 10),
            new("src/App/B.cs", 10),
            new("src/App/C.cs", 10)
        ];
        HashSet<string> projectFolders = new(StringComparer.Ordinal) { "src/App" };

        IReadOnlyList<CodeViolation> violations = CodeEnforcerEngine.Check(files, projectFolders, config);

        CodeViolation violation = Assert.Single(violations);
        Assert.Equal("CE0004", violation.Rule);
        Assert.Equal("src/App", violation.Path);
    }

    [Fact]
    public void AllowsProjectFolderAboveFileLimitWhenExcluded()
    {
        CodeEnforcerConfig config = new() { MaxFilesInProjectFolder = 2 };
        config.ProjectFolderExclusions.Add(new PathExclusion { Path = "src/App" });
        HashSet<string> projectFolders = new(StringComparer.Ordinal) { "src/App" };

        IReadOnlyList<CodeViolation> violations = CodeEnforcerEngine.Check(
            [
                new CodeFile("src/App/A.cs", 10),
                new CodeFile("src/App/B.cs", 10),
                new CodeFile("src/App/C.cs", 10)
            ],
            projectFolders,
            config);

        Assert.Empty(violations);
    }

    [Fact]
    public void ReportsMoreThanTwoSingleCSharpFileFolders()
    {
        CodebaseSnapshot snapshot = Snapshot(
            "src/A/Only.cs",
            "src/B/Only.cs",
            "src/C/Only.cs");

        IReadOnlyList<CodeViolation> violations = CodeEnforcerEngine.Check(snapshot, new CodeEnforcerConfig());

        CodeViolation violation = Assert.Single(violations);
        Assert.Equal("CE0005", violation.Rule);
        Assert.Equal(".", violation.Path);
        Assert.Contains("src/A", violation.Message);
        Assert.Contains("src/B", violation.Message);
        Assert.Contains("src/C", violation.Message);
    }

    [Fact]
    public void AllowsTwoSingleCSharpFileFolders()
    {
        CodebaseSnapshot snapshot = Snapshot(
            "src/A/Only.cs",
            "src/B/Only.cs");

        IReadOnlyList<CodeViolation> violations = CodeEnforcerEngine.Check(snapshot, new CodeEnforcerConfig());

        Assert.Empty(violations);
    }

    [Fact]
    public void DoesNotCountFoldersWithOtherTrackedFiles()
    {
        CodebaseSnapshot snapshot = Snapshot(
            "src/A/Only.cs",
            "src/A/readme.md",
            "src/B/Only.cs",
            "src/B/Other.txt",
            "src/C/Only.cs",
            "src/C/Project.csproj");
        CodeEnforcerConfig config = new() { MaxFilesInProjectFolder = 15 };

        IReadOnlyList<CodeViolation> violations = CodeEnforcerEngine.Check(snapshot, config);

        Assert.Empty(violations);
    }

    private static IReadOnlyList<CodeViolation> Check(IReadOnlyList<CodeFile> files, CodeEnforcerConfig config) =>
        CodeEnforcerEngine.Check(files, config);

    private static CodebaseSnapshot Snapshot(params string[] paths)
    {
        CodeFile[] files = paths
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => new CodeFile(path, 10))
            .ToArray();
        HashSet<string> projectFolders = paths
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(PathUtility.GetDirectory)
            .ToHashSet(StringComparer.Ordinal);

        return new CodebaseSnapshot(files, projectFolders, paths);
    }
}
