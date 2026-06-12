using CodeEnforcer;

namespace CodeEnforcer.Tests;

public sealed class JustificationStoreTests : IDisposable
{
    private readonly string root;
    private readonly string configPath;
    private readonly string justificationsPath;

    public JustificationStoreTests()
    {
        root = Path.Combine(Path.GetTempPath(), "code-enforcer-justifications-tests", Guid.NewGuid().ToString("N"));
        string configDirectory = Path.Combine(root, ".config", "code-enforcer");
        Directory.CreateDirectory(configDirectory);
        configPath = Path.Combine(configDirectory, "code-enforcer.json");
        justificationsPath = Path.Combine(configDirectory, "justifications.json");
        File.WriteAllText(configPath, """
            {
              "version": 1,
              "maxFilesPerDir": 15,
              "maxFilesPerRootDir": 5,
              "maxLinesSoft": 300,
              "maxLinesHard": 500
            }
            """);
    }

    [Fact]
    public void AddCreatesJustificationsFileWhenMissing()
    {
        JustificationStore store = JustificationStore.Open(configPath);

        store.Add(JustificationEntryType.File, "src/App/Large.cs", "Legacy file.");
        store.Save();

        Assert.True(File.Exists(justificationsPath));
        string json = File.ReadAllText(justificationsPath);
        Assert.Contains("\"files\"", json);
        Assert.Contains("\"path\": \"src/App/Large.cs\"", json);
        Assert.Contains("\"justification\": \"Legacy file.\"", json);
    }

    [Fact]
    public void FindReadsExistingEntry()
    {
        WriteJustifications("""
            {
              "version": 1,
              "files": [
                {
                  "path": "src/App/Large.cs",
                  "justification": "Legacy file."
                }
              ],
              "folders": [],
              "rootFolders": []
            }
            """);

        JustificationStore store = JustificationStore.Open(configPath);

        PathExclusion? entry = store.Find(JustificationEntryType.File, "src/App/Large.cs");

        Assert.NotNull(entry);
        Assert.Equal("Legacy file.", entry.Justification);
    }

    [Fact]
    public void UpdateChangesPathAndJustification()
    {
        WriteJustifications("""
            {
              "version": 1,
              "files": [
                {
                  "path": "src/App/Large.cs",
                  "justification": "Legacy file."
                }
              ],
              "folders": [],
              "rootFolders": []
            }
            """);
        JustificationStore store = JustificationStore.Open(configPath);

        store.Update(
            JustificationEntryType.File,
            "src/App/Large.cs",
            "src/App/SplitLater.cs",
            "Scheduled for split.",
            clearJustification: false);
        store.Save();

        CodeEnforcerJustifications saved = CodeEnforcerJustifications.Load(Path.GetDirectoryName(configPath)!);
        PathExclusion entry = Assert.Single(saved.Files);
        Assert.Equal("src/App/SplitLater.cs", entry.Path);
        Assert.Equal("Scheduled for split.", entry.Justification);
    }

    [Fact]
    public void RemoveDeletesEntry()
    {
        WriteJustifications("""
            {
              "version": 1,
              "files": [],
              "folders": [
                {
                  "path": "src/App/Generated"
                }
              ],
              "rootFolders": []
            }
            """);
        JustificationStore store = JustificationStore.Open(configPath);

        store.Remove(JustificationEntryType.Folder, "src/App/Generated");
        store.Save();

        CodeEnforcerJustifications saved = CodeEnforcerJustifications.Load(Path.GetDirectoryName(configPath)!);
        Assert.Empty(saved.Folders);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private void WriteJustifications(string json) =>
        File.WriteAllText(justificationsPath, json);
}
