# CodeEnforcer

CodeEnforcer is a repository-local C# structure gate. It scans tracked C# files and fails when file length, folder size, or project-root folder size crosses configured limits without an explicit justification.

The tool is designed for CI and pre-commit usage: exclusions are centralized in `.config/code-enforcer/justifications.json` so structural debt is visible in review.

## Rules

- `CE0001`: A tracked C# file exceeds `maxLinesSoft` and is not listed in `justifications.json`.
- `CE0002`: A tracked C# file exceeds `maxLinesHard` and its exclusion has no non-empty justification.
- `CE0003`: A folder contains more tracked C# files than `maxFilesPerDir`.
- `CE0004`: A folder containing a `.csproj` contains more tracked C# files than `maxFilesPerRootDir`.

Generated and build-output files are skipped:

- `bin/**`
- `obj/**`
- `*.g.cs`
- `*.Designer.cs`

## Run

From a repository that contains `.config/code-enforcer/code-enforcer.json`:

```powershell
code-enforcer
```

Run from this source checkout:

```powershell
dotnet run --project src/CodeEnforcer -- --root . --config .config/code-enforcer/code-enforcer.json
```

Run the complete verification gate:

```powershell
./scripts/verify.ps1
```

## Build the .NET tool

```powershell
./scripts/build-tool.ps1
dotnet tool install --global --add-source ./artifacts/packages CodeEnforcer
```

The installed command is:

```powershell
code-enforcer
```

## Configuration

CodeEnforcer walks from the current directory to parent directories until it finds:

```text
.config/code-enforcer/code-enforcer.json
```

The same folder must also contain:

```text
.config/code-enforcer/justifications.json
```

See [configuration](docs/configuration.md) for schema details and examples.

## Development

See [development](docs/development.md) for local verification, git hook setup, and package build instructions.
