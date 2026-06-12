# Configuration

CodeEnforcer loads configuration from `.config/code-enforcer/code-enforcer.json` and exclusions from the sibling `justifications.json` file.

This page describes CLI configuration. Roslyn analyzer configuration uses `.editorconfig`; see [analyzer](analyzer.md).

When `--config` is not supplied, the tool walks from the current directory to parent directories until it finds `.config/code-enforcer/code-enforcer.json`. It then scans the containing git repository unless `--root` is supplied.

Use `code-enforcer init` in a git repository to create the default config files and install the pre-commit hook.

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

`CE0005` has no config field: the CLI rejects repositories with more than two folders that contain exactly one tracked C# file and no other tracked files. This commit-time rule is fixed because it is intended to catch one-file-folder gaming instead of creating another bypass.

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

## Manage Entries From The CLI

Prefer the CLI over hand-editing JSON:

```powershell
code-enforcer justifications list
code-enforcer justifications list --type file
code-enforcer justifications show --type file --path src/App/Large.cs
code-enforcer justifications add --type file --path src/App/Large.cs --justification "Scheduled for split"
code-enforcer justifications update --type file --path src/App/Large.cs --new-path src/App/Legacy.cs
code-enforcer justifications update --type file --path src/App/Legacy.cs --clear-justification
code-enforcer justifications remove --type file --path src/App/Legacy.cs
```

Use `--type file`, `--type folder`, or `--type root-folder`. The `exceptions` command branch is an alias for `justifications`.

`add` creates `justifications.json` when `.config/code-enforcer/code-enforcer.json` exists and the sibling justifications file is missing.
