using System.Collections.Immutable;
using System.Globalization;
using CodeEnforcer.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeEnforcer.Analyzers.Tests;

public sealed class CodeEnforcerAnalyzerTests
{
    [Fact]
    public async Task DoesNotReportForSmallFile()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [new TestSource("src/App/Small.cs", "namespace App; public sealed class Small { }")],
            []);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsFileAboveSoftLimitWithoutExclusion()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [new TestSource("src/App/Large.cs", Lines(4))],
            [("dotnet_code_enforcer_max_lines_soft", "3")]);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CE0001", diagnostic.Id);
        Assert.Equal("src/App/Large.cs", diagnostic.GetMessage(CultureInfo.InvariantCulture).Split(' ')[0]);
    }

    [Fact]
    public async Task AllowsFileAboveSoftLimitWhenExcluded()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [new TestSource("src/App/Large.cs", Lines(4))],
            [
                ("dotnet_code_enforcer_max_lines_soft", "3"),
                ("dotnet_code_enforcer_file_exclusions", "src/App/Large.cs")
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsHardLimitWhenExclusionHasNoJustification()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [new TestSource("src/App/Giant.cs", Lines(5))],
            [
                ("dotnet_code_enforcer_max_lines_soft", "3"),
                ("dotnet_code_enforcer_max_lines_hard", "4"),
                ("dotnet_code_enforcer_file_exclusions", "src/App/Giant.cs")
            ]);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CE0002", diagnostic.Id);
    }

    [Fact]
    public async Task AllowsHardLimitWhenJustified()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [new TestSource("src/App/Giant.cs", Lines(5))],
            [
                ("dotnet_code_enforcer_max_lines_soft", "3"),
                ("dotnet_code_enforcer_max_lines_hard", "4"),
                ("dotnet_code_enforcer_hard_file_justifications", "src/App/Giant.cs=Legacy split pending")
            ]);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsFolderAboveFileLimit()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [
                new TestSource("src/App/A.cs", "class A { }"),
                new TestSource("src/App/B.cs", "class B { }"),
                new TestSource("src/App/C.cs", "class C { }")
            ],
            [("dotnet_code_enforcer_max_files_per_dir", "2")]);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CE0003", diagnostic.Id);
        Assert.Contains("src/App", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsProjectFolderAboveFileLimit()
    {
        string projectDirectory = "C:/repo/src/App";
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [
                new TestSource("C:/repo/src/App/A.cs", "class A { }"),
                new TestSource("C:/repo/src/App/B.cs", "class B { }"),
                new TestSource("C:/repo/src/App/C.cs", "class C { }")
            ],
            [
                ("build_property.ProjectDir", projectDirectory),
                ("dotnet_code_enforcer_max_files_per_root_dir", "2")
            ]);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CE0004", diagnostic.Id);
        Assert.Contains(".", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SkipsGeneratedAndBuildOutputFiles()
    {
        Diagnostic[] diagnostics = await RunAnalyzerAsync(
            [
                new TestSource("src/App/Generated.g.cs", Lines(5)),
                new TestSource("src/App/bin/Debug/Generated.cs", Lines(5))
            ],
            [("dotnet_code_enforcer_max_lines_soft", "3")]);

        Assert.Empty(diagnostics);
    }

    private static async Task<Diagnostic[]> RunAnalyzerAsync(
        IReadOnlyList<TestSource> sources,
        IReadOnlyList<(string Key, string Value)> options)
    {
        SyntaxTree[] syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source.Text, path: source.Path))
            .ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            syntaxTrees,
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        AnalyzerOptions analyzerOptions = new(
            ImmutableArray<AdditionalText>.Empty,
            new TestAnalyzerConfigOptionsProvider(options));
        CompilationWithAnalyzers analyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new CodeEnforcerStructureAnalyzer()),
            new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: false));

        ImmutableArray<Diagnostic> diagnostics = await analyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.OrderBy(diagnostic => diagnostic.Id).ToArray();
    }

    private static string Lines(int count) =>
        string.Join(Environment.NewLine, Enumerable.Range(1, count).Select(line => "// " + line));

    private sealed class TestSource
    {
        public TestSource(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }

        public string Text { get; }
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions globalOptions;
        private readonly AnalyzerConfigOptions treeOptions;

        public TestAnalyzerConfigOptionsProvider(IEnumerable<(string Key, string Value)> options)
        {
            (string Key, string Value)[] optionArray = options.ToArray();
            globalOptions = new TestAnalyzerConfigOptions(
                optionArray.Where(option => option.Key.StartsWith("build_property.", StringComparison.Ordinal)));
            treeOptions = new TestAnalyzerConfigOptions(
                optionArray.Where(option => !option.Key.StartsWith("build_property.", StringComparison.Ordinal)));
        }

        public override AnalyzerConfigOptions GlobalOptions => globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => treeOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static readonly TestAnalyzerConfigOptions Empty = new([]);

        private readonly Dictionary<string, string> values;

        public TestAnalyzerConfigOptions(IEnumerable<(string Key, string Value)> values)
        {
            this.values = values.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        public override bool TryGetValue(string key, out string value) =>
            values.TryGetValue(key, out value!);
    }
}
