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

## Build the tool package

```powershell
./scripts/build-tool.ps1
```

The package is written to `artifacts/packages`.

Install from the local package source:

```powershell
dotnet tool install --global --add-source ./artifacts/packages CodeEnforcer
```

Update an existing global install:

```powershell
dotnet tool update --global --add-source ./artifacts/packages CodeEnforcer
```
