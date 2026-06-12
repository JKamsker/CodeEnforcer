using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeEnforcer;

internal sealed class JustificationStore
{
    private static readonly JsonSerializerOptions WriteOptions = new(JsonConfiguration.SerializerOptions)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string path;

    private JustificationStore(string path, CodeEnforcerJustifications justifications)
    {
        this.path = path;
        Justifications = justifications;
    }

    public CodeEnforcerJustifications Justifications { get; }

    public static JustificationStore Open(string? configPath)
    {
        string resolvedConfigPath = ResolveConfigPath(configPath);
        string? configDirectory = Path.GetDirectoryName(resolvedConfigPath);
        if (configDirectory is null)
        {
            throw new CodeEnforcerException("Config path has no directory.", ExitCodes.InputError);
        }

        string justificationsPath = Path.Combine(configDirectory, "justifications.json");
        CodeEnforcerJustifications justifications = File.Exists(justificationsPath)
            ? CodeEnforcerJustifications.Load(configDirectory)
            : new CodeEnforcerJustifications();

        return new JustificationStore(justificationsPath, justifications);
    }

    public IReadOnlyList<PathExclusion> GetEntries(JustificationEntryType type) =>
        GetMutableEntries(type);

    public PathExclusion? Find(JustificationEntryType type, string path)
    {
        string normalizedPath = NormalizePath(path);
        return GetMutableEntries(type)
            .FirstOrDefault(entry => string.Equals(
                NormalizePath(entry.Path),
                normalizedPath,
                StringComparison.Ordinal));
    }

    public PathExclusion Add(JustificationEntryType type, string path, string? justification)
    {
        if (Find(type, path) is not null)
        {
            throw new CodeEnforcerException(
                $"{JustificationEntryTypes.Format(type)} justification already exists for {path}.",
                ExitCodes.InputError);
        }

        PathExclusion entry = new()
        {
            Path = NormalizePath(path),
            Justification = NormalizeJustification(justification)
        };
        GetMutableEntries(type).Add(entry);
        return entry;
    }

    public PathExclusion Update(
        JustificationEntryType type,
        string path,
        string? newPath,
        string? justification,
        bool clearJustification)
    {
        PathExclusion entry = Find(type, path) ?? throw NotFound(type, path);
        if (!string.IsNullOrWhiteSpace(newPath))
        {
            PathExclusion? duplicate = Find(type, newPath);
            if (duplicate is not null && !ReferenceEquals(duplicate, entry))
            {
                throw new CodeEnforcerException(
                    $"{JustificationEntryTypes.Format(type)} justification already exists for {newPath}.",
                    ExitCodes.InputError);
            }

            entry.Path = NormalizePath(newPath);
        }

        if (clearJustification)
        {
            entry.Justification = null;
        }
        else if (justification is not null)
        {
            entry.Justification = NormalizeJustification(justification);
        }

        return entry;
    }

    public PathExclusion Remove(JustificationEntryType type, string path)
    {
        List<PathExclusion> entries = GetMutableEntries(type);
        PathExclusion entry = Find(type, path) ?? throw NotFound(type, path);
        entries.Remove(entry);
        return entry;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllText(path, JsonSerializer.Serialize(Justifications, WriteOptions) + Environment.NewLine);
    }

    private static string ResolveConfigPath(string? configPath)
    {
        string path = configPath is null
            ? RepositoryPaths.DiscoverConfigPath(Environment.CurrentDirectory)
            : Path.IsPathRooted(configPath)
                ? configPath
                : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configPath));

        if (!File.Exists(path))
        {
            throw new CodeEnforcerException($"Config file does not exist: {path}", ExitCodes.InputError);
        }

        return path;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new CodeEnforcerException("Path must not be empty.", ExitCodes.InputError);
        }

        return path.Trim() == "." ? "." : PathUtility.Normalize(path);
    }

    private static string? NormalizeJustification(string? justification)
    {
        string? trimmed = justification?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static CodeEnforcerException NotFound(JustificationEntryType type, string path) =>
        new($"{JustificationEntryTypes.Format(type)} justification does not exist for {path}.", ExitCodes.InputError);

    private List<PathExclusion> GetMutableEntries(JustificationEntryType type) =>
        type switch
        {
            JustificationEntryType.File => Justifications.Files,
            JustificationEntryType.Folder => Justifications.Folders,
            JustificationEntryType.RootFolder => Justifications.RootFolders,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}
