using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeEnforcer.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CodeEnforcerStructureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            CodeEnforcerDiagnosticDescriptors.FileAboveSoftLimit,
            CodeEnforcerDiagnosticDescriptors.FileAboveHardLimit,
            CodeEnforcerDiagnosticDescriptors.FolderAboveFileLimit,
            CodeEnforcerDiagnosticDescriptors.ProjectFolderAboveFileLimit);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        AnalyzerConfig globalConfig = AnalyzerConfig.From(
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions);
        List<CodeFileInfo> files = CollectFiles(context, globalConfig);

        foreach (CodeFileInfo file in files.OrderBy(file => file.Path, System.StringComparer.Ordinal))
        {
            AnalyzerConfig config = GetConfig(context, file);
            ReportFileDiagnostics(context, file, config);
        }

        foreach (IGrouping<string, CodeFileInfo> folder in files
                     .GroupBy(file => file.Folder)
                     .OrderBy(group => group.Key, System.StringComparer.Ordinal))
        {
            AnalyzerConfig config = GetConfig(context, folder.First());
            ReportFolderDiagnostics(context, folder, config);
            ReportProjectFolderDiagnostics(context, folder, config);
        }
    }

    private static List<CodeFileInfo> CollectFiles(
        CompilationAnalysisContext context,
        AnalyzerConfig config)
    {
        List<CodeFileInfo> files = new();
        foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string path = PathRules.ToProjectRelativePath(syntaxTree.FilePath, config.ProjectDirectory);
            if (PathRules.IsGeneratedOrBuildOutput(path))
            {
                continue;
            }

            SourceText text = syntaxTree.GetText(context.CancellationToken);
            files.Add(new CodeFileInfo(path, text.Lines.Count, syntaxTree));
        }

        return files;
    }

    private static void ReportFileDiagnostics(
        CompilationAnalysisContext context,
        CodeFileInfo file,
        AnalyzerConfig config)
    {
        if (file.LineCount <= config.SoftLineLimit)
        {
            return;
        }

        bool hasSoftExclusion = config.FileExclusions.IsMatch(file.Path) ||
            config.HardFileJustifications.IsMatch(file.Path);
        if (!hasSoftExclusion)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CodeEnforcerDiagnosticDescriptors.FileAboveSoftLimit,
                CreateFileLocation(file),
                file.Path,
                file.LineCount,
                config.SoftLineLimit));
            return;
        }

        if (file.LineCount > config.HardLineLimit &&
            !config.HardFileJustifications.IsMatch(file.Path))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                CodeEnforcerDiagnosticDescriptors.FileAboveHardLimit,
                CreateFileLocation(file),
                file.Path,
                file.LineCount,
                config.HardLineLimit));
        }
    }

    private static AnalyzerConfig GetConfig(CompilationAnalysisContext context, CodeFileInfo file) =>
        AnalyzerConfig.From(
            context.Options.AnalyzerConfigOptionsProvider.GlobalOptions,
            context.Options.AnalyzerConfigOptionsProvider.GetOptions(file.SyntaxTree));

    private static void ReportFolderDiagnostics(
        CompilationAnalysisContext context,
        IGrouping<string, CodeFileInfo> folder,
        AnalyzerConfig config)
    {
        int fileCount = folder.Count();
        if (fileCount <= config.MaxFilesPerFolder || config.FolderExclusions.IsMatch(folder.Key))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CodeEnforcerDiagnosticDescriptors.FolderAboveFileLimit,
            CreateFileLocation(folder.First()),
            folder.Key,
            fileCount,
            config.MaxFilesPerFolder));
    }

    private static void ReportProjectFolderDiagnostics(
        CompilationAnalysisContext context,
        IGrouping<string, CodeFileInfo> folder,
        AnalyzerConfig config)
    {
        if (folder.Key != "." ||
            config.ProjectDirectory is null ||
            config.ProjectFolderExclusions.IsMatch(folder.Key))
        {
            return;
        }

        int fileCount = folder.Count();
        if (fileCount <= config.MaxFilesInProjectFolder)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            CodeEnforcerDiagnosticDescriptors.ProjectFolderAboveFileLimit,
            CreateFileLocation(folder.First()),
            folder.Key,
            fileCount,
            config.MaxFilesInProjectFolder));
    }

    private static Location CreateFileLocation(CodeFileInfo file)
    {
        SourceText text = file.SyntaxTree.GetText();
        TextSpan span = text.Length == 0 ? default : new TextSpan(0, 1);
        return Location.Create(file.SyntaxTree, span);
    }
}
