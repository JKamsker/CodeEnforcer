# Configuration

CodeEnforcer loads configuration from `.config/code-enforcer/code-enforcer.json` and exclusions from the sibling `justifications.json` file.

When `--config` is not supplied, the tool walks from the current directory to parent directories until it finds `.config/code-enforcer/code-enforcer.json`. It then scans the containing git repository unless `--root` is supplied.

## code-enforcer.json

```json
{
  "version": 1,
  "maxFilesPerDir": 15,
  "maxFilesPerRootDir": 5,
  "maxLinesSoft": 300,
  "maxLinesHard": 500
}
```

Fields:

- `maxFilesPerDir`: Maximum tracked C# files allowed in one folder.
- `maxFilesPerRootDir`: Maximum tracked C# files allowed beside a `.csproj`. Defaults to `maxFilesPerDir` when omitted.
- `maxLinesSoft`: Line count that requires a file exclusion.
- `maxLinesHard`: Line count that requires a file exclusion with a non-empty justification.

All paths use forward slashes internally. Windows backslashes are accepted in config and CLI inputs.

## justifications.json

```json
{
  "version": 1,
  "files": [
    {
      "path": "src/App/LegacyFile.cs",
      "justification": "Scheduled for split after the persistence refactor."
    }
  ],
  "folders": [
    {
      "path": "src/App/GeneratedAdapters"
    }
  ],
  "rootFolders": [
    {
      "path": "src/App"
    }
  ]
}
```

Sections:

- `files`: C# files allowed to exceed `maxLinesSoft`. Files above `maxLinesHard` must include `justification`.
- `folders`: Folders allowed to exceed `maxFilesPerDir`.
- `rootFolders`: Project folders allowed to exceed `maxFilesPerRootDir`.

`*` matches within one path segment. `**` matches across path segments.
