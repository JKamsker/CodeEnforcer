# CodeEnforcer

[![CI](https://github.com/JKamsker/CodeEnforcer/actions/workflows/ci.yml/badge.svg)](https://github.com/JKamsker/CodeEnforcer/actions/workflows/ci.yml)
[![CodeEnforcer NuGet](https://img.shields.io/nuget/vpre/CodeEnforcer?label=tool)](https://www.nuget.org/packages/CodeEnforcer)
[![CodeEnforcer downloads](https://img.shields.io/nuget/dt/CodeEnforcer?label=tool%20downloads)](https://www.nuget.org/packages/CodeEnforcer)
[![Analyzers NuGet](https://img.shields.io/nuget/vpre/CodeEnforcer.Analyzers?label=analyzer)](https://www.nuget.org/packages/CodeEnforcer.Analyzers)
[![Analyzers downloads](https://img.shields.io/nuget/dt/CodeEnforcer.Analyzers?label=analyzer%20downloads)](https://www.nuget.org/packages/CodeEnforcer.Analyzers)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

CodeEnforcer is a repository-local C# structure gate. It scans tracked files and fails when file length, folder size, project-root folder size, or one-file-folder patterns cross enforced limits.

The CLI is designed for CI and pre-commit usage: exclusions are centralized in `.config/code-enforcer/justifications.json` so structural debt is visible in review. The repository also ships `CodeEnforcer.Analyzers`, a Roslyn analyzer package for compiler and IDE feedback.

## What You Get

- A `code-enforcer` .NET tool for repository-wide checks.
- A `CodeEnforcer.Analyzers` package for compile-time and IDE feedback.
- A one-command `init` flow that writes config files and installs the pre-commit hook.
- CLI CRUD for exceptions and justifications, so teams do not need to hand-edit JSON.
- Shared CLI/analyzer rule IDs: `CE0001` through `CE0004`.
- A commit-time CLI-only anti-gaming rule: `CE0005`.

## Install

Install the CLI tool:

```powershell
dotnet tool install --global CodeEnforcer --prerelease
```

Add the analyzer to a project:

```powershell
dotnet add package CodeEnforcer.Analyzers --prerelease
```

## Rules

- `CE0001`: A tracked C# file exceeds `maxLinesSoft` and is not listed in `justifications.json`.
- `CE0002`: A tracked C# file exceeds `maxLinesHard` and its exclusion has no non-empty justification.
- `CE0003`: A folder contains more tracked C# files than `maxFilesPerDir`.
- `CE0004`: A folder containing a `.csproj` contains more tracked C# files than `maxFilesPerRootDir`.
- `CE0005`: The repository contains more than two folders that have exactly one tracked C# file and no other tracked files. This is CLI-only because it needs `git ls-files`.

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

Or explicitly:

```powershell
code-enforcer check
```

Run from this source checkout:

```powershell
dotnet run --project src/CodeEnforcer -- check --root . --config .config/code-enforcer/code-enforcer.json
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

## Initialize A Repository

Create the default config files and install the git pre-commit hook:

```powershell
code-enforcer init
```

This creates:

- `.config/code-enforcer/code-enforcer.json`
- `.config/code-enforcer/justifications.json`
- `.githooks/pre-commit`

It also runs `git config core.hooksPath .githooks`. Existing files are kept unless `--force` is supplied.

The same build script also creates `CodeEnforcer.Analyzers.0.1.0.nupkg`.

## Roslyn Analyzer

Install the analyzer package into a consuming project:

```powershell
dotnet add package CodeEnforcer.Analyzers --source ./artifacts/packages
```

Configure analyzer limits and exclusions in `.editorconfig`. See [analyzer](docs/analyzer.md) for install, configuration, and CLI differences.

## Configuration

CodeEnforcer walks from the current directory to parent directories until it finds:

```text
.config/code-enforcer/code-enforcer.json
```

The same folder must also contain:

```text
.config/code-enforcer/justifications.json
```

See [configuration](docs/configuration.md) for CLI schema details and examples.

## Manage Justifications

Use the CLI to create, read, update, and delete entries in `.config/code-enforcer/justifications.json`:

```powershell
code-enforcer justifications add --type file --path src/App/Large.cs --justification "Scheduled for split"
code-enforcer justifications list
code-enforcer justifications show --type file --path src/App/Large.cs
code-enforcer justifications update --type file --path src/App/Large.cs --justification "Split after adapter cleanup"
code-enforcer justifications remove --type file --path src/App/Large.cs
```

`exceptions` is an alias for `justifications`, so `code-enforcer exceptions add ...` works the same way.

## Development

See [development](docs/development.md) for local verification, git hook setup, and package build instructions.
