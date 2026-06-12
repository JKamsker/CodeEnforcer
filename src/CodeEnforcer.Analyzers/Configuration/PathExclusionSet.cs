using System.Collections.Generic;
using System.Linq;

namespace CodeEnforcer.Analyzers;

internal sealed class PathExclusionSet
{
    public static readonly PathExclusionSet Empty = new(Enumerable.Empty<string>());

    private readonly string[] patterns;

    private PathExclusionSet(IEnumerable<string> patterns)
    {
        this.patterns = patterns
            .Select(PathRules.Normalize)
            .Where(pattern => pattern.Length > 0)
            .Distinct()
            .ToArray();
    }

    public static PathExclusionSet FromPaths(IEnumerable<string> patterns) => new(patterns);

    public bool IsMatch(string path)
    {
        foreach (string pattern in patterns)
        {
            if (PathRules.IsMatch(path, pattern))
            {
                return true;
            }
        }

        return false;
    }
}
