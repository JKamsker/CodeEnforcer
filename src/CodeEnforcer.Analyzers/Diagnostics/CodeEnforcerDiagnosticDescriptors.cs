using Microsoft.CodeAnalysis;

namespace CodeEnforcer.Analyzers;

internal static class CodeEnforcerDiagnosticDescriptors
{
    private const string Category = "Structure";
    private static readonly string[] CompilationEndTags = [WellKnownDiagnosticTags.CompilationEnd];

    public static readonly DiagnosticDescriptor FileAboveSoftLimit = new(
        "CE0001",
        "C# file exceeds the soft line limit",
        "{0} has {1} lines, exceeding the soft limit of {2}. Add an exclusion or split the file.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Large C# files should be split or explicitly excluded.",
        customTags: CompilationEndTags);

    public static readonly DiagnosticDescriptor FileAboveHardLimit = new(
        "CE0002",
        "C# file exceeds the hard line limit without justification",
        "{0} has {1} lines, exceeding the hard limit of {2}. Add a non-empty hard-limit justification or split the file.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Very large C# files require a non-empty justification.",
        customTags: CompilationEndTags);

    public static readonly DiagnosticDescriptor FolderAboveFileLimit = new(
        "CE0003",
        "Folder contains too many C# files",
        "{0} contains {1} C# files, exceeding the folder limit of {2}. Group files into subdirectories or add an exclusion.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Folders should not accumulate too many C# files.",
        customTags: CompilationEndTags);

    public static readonly DiagnosticDescriptor ProjectFolderAboveFileLimit = new(
        "CE0004",
        "Project folder contains too many C# files",
        "{0} contains a project and {1} C# files, exceeding the project-folder limit of {2}. Move implementation files into subdirectories or add an exclusion.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Project folders should keep implementation files in subdirectories.",
        customTags: CompilationEndTags);
}
