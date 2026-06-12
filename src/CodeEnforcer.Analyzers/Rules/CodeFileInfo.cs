using Microsoft.CodeAnalysis;

namespace CodeEnforcer.Analyzers;

internal sealed class CodeFileInfo
{
    public CodeFileInfo(string path, int lineCount, SyntaxTree syntaxTree)
    {
        Path = path;
        LineCount = lineCount;
        SyntaxTree = syntaxTree;
    }

    public string Path { get; }

    public int LineCount { get; }

    public SyntaxTree SyntaxTree { get; }

    public string Folder => PathRules.Folder(Path);
}
