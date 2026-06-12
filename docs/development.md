# Development

## Prerequisites

- .NET SDK matching `global.json`.
- Git available on `PATH`.
- PowerShell 7 or Windows PowerShell for repository scripts.

## Verify locally

Run the full local gate:

```powershell
./scripts/verify.ps1
```

The script performs:

- `dotnet restore CodeEnforcer.slnx --locked-mode`
- `dotnet build CodeEnforcer.slnx --configuration Release --no-restore`
- `dotnet test CodeEnforcer.slnx --configuration Release --no-build`
- `./scripts/check-csharp-file-lines.ps1`

## Git hooks

Install the committed pre-commit hook:

```powershell
./scripts/install-git-hooks.ps1
```

The hook runs `scripts/verify.ps1`.

## CLI Features

The CLI is feature-command based:

- `code-enforcer` or `code-enforcer check`: Run the repository check.
- `code-enforcer init`: Create `.config/code-enforcer` defaults, create `.githooks/pre-commit`, and configure `core.hooksPath`.
- `code-enforcer justifications ...`: List, show, add, update, and remove exception entries in `justifications.json`.
- `code-enforcer exceptions ...`: Alias for `justifications`.

For backwards compatibility, root-level check options still work:

```powershell
code-enforcer --root . --config .config/code-enforcer/code-enforcer.json
```

## Build the tool package

```powershell
./scripts/build-tool.ps1
```

The CLI and analyzer packages are written to `artifacts/packages`.

Build a specific package version:

```powershell
./scripts/build-tool.ps1 -Version 0.1.0-ci.local
```

Install from the local package source:

```powershell
dotnet tool install --global --add-source ./artifacts/packages CodeEnforcer
```

Update an existing global install:

```powershell
dotnet tool update --global --add-source ./artifacts/packages CodeEnforcer
```

Install the analyzer package into a local consumer project:

```powershell
dotnet add package CodeEnforcer.Analyzers --source ./artifacts/packages
```

## NuGet Publishing

CI publishes packages to NuGet.org only for pushes to `main` or `v*` tags in `JKamsker/CodeEnforcer`.

- `main` pushes publish prerelease versions: `0.1.0-ci.<run_number>`.
- `v*` tags publish stable versions: `v1.2.3` becomes `1.2.3`.
- The NuGet API key is fetched through Bitwarden using `secrets.BW_ACCESS_TOKEN`.
