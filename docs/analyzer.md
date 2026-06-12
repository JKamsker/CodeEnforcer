# Roslyn Analyzer

`CodeEnforcer.Analyzers` reports the same rule IDs as the CLI during C# compilation:

- `CE0001`: File exceeds the soft line limit without an exclusion.
- `CE0002`: File exceeds the hard line limit without a non-empty justification.
- `CE0003`: Folder contains too many C# files.
- `CE0004`: Project folder contains too many C# files.

The analyzer evaluates files visible to the current compilation. It does not call `git ls-files`, discover repository config files, or scan files that are not part of the project being compiled.

## Install

From a local package build:

```powershell
./scripts/build-tool.ps1
dotnet add package CodeEnforcer.Analyzers --source ./artifacts/packages
```

For direct project references while developing:

```xml
<ItemGroup>
  <ProjectReference Include="..\CodeEnforcer.Analyzers\CodeEnforcer.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Configure

Use `.editorconfig`:

```editorconfig
[*.cs]
dotnet_code_enforcer_max_lines_soft = 300
dotnet_code_enforcer_max_lines_hard = 500
dotnet_code_enforcer_max_files_per_dir = 15
dotnet_code_enforcer_max_files_per_root_dir = 5
dotnet_code_enforcer_file_exclusions = src/App/Legacy.cs;src/App/Generated/**/*.cs
dotnet_code_enforcer_hard_file_justifications = src/App/Giant.cs=Scheduled for split
dotnet_code_enforcer_folder_exclusions = src/App/Adapters
dotnet_code_enforcer_root_folder_exclusions = .
```

List options accept `;` or `,` separators. `*` matches within one path segment, and `**` matches across path segments.

The analyzer package includes `buildTransitive` MSBuild props that expose `ProjectDir` and `MSBuildProjectDirectory` to the compiler. This lets `CE0004` identify files directly in the project directory.

## Differences From The CLI

The CLI is repository-oriented:

- It scans tracked files from `git ls-files`.
- It loads `.config/code-enforcer/code-enforcer.json`.
- It loads `.config/code-enforcer/justifications.json`.

The analyzer is compiler-oriented:

- It scans syntax trees in the current compilation.
- It reads `.editorconfig` analyzer options.
- It reports diagnostics in build, IDE, and CI compiler output.

Use the CLI for repository-wide enforcement and the analyzer for immediate project/IDE feedback.
