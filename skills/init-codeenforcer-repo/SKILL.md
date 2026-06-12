---
name: init-codeenforcer-repo
description: Initialize a git repository with CodeEnforcer. Use when setting up a new or existing repo with git, a local dotnet tool manifest, the CodeEnforcer CLI as a local tool by default, CodeEnforcer config files, and pre-commit hooks.
---

# Init CodeEnforcer Repo

## Workflow

Prefer the bundled script for repeatable setup:

```powershell
./skills/init-codeenforcer-repo/scripts/init-codeenforcer-repo.ps1 -Path .
```

Use `-Force` only when the user explicitly wants existing CodeEnforcer config or hook files overwritten.

## What The Setup Must Do

1. Ensure the target directory exists.
2. Ensure it is inside a git repository; run `git init` when it is not.
3. Resolve the repository root with `git rev-parse --show-toplevel`.
4. Ensure a local dotnet tool manifest exists.
5. Install or update `CodeEnforcer` as a local dotnet tool.
6. Run `dotnet tool run code-enforcer -- init --root <repo-root>`.
7. Verify `git config core.hooksPath` is `.githooks`.
8. Run `dotnet tool run code-enforcer -- check --root <repo-root> --config <repo-root>/.config/code-enforcer/code-enforcer.json`.

## Defaults

- Install CodeEnforcer as a local tool, not a global tool.
- Use NuGet.org as the package source unless the user supplies a local package source.
- Include prerelease packages by default because CodeEnforcer may not have a stable release yet.
- Preserve existing config and hook files unless the user asks to overwrite them.
- Do not add entries to `justifications` during normal setup. Justifications are an absolute exception for explicit, case-specific user decisions and are very discouraged as a default workflow.

## Manual Commands

Use these when the script is inappropriate:

```powershell
git init
dotnet new tool-manifest
dotnet tool install CodeEnforcer --local --prerelease
dotnet tool run code-enforcer -- init --root .
dotnet tool run code-enforcer -- check --root . --config .config/code-enforcer/code-enforcer.json
```

If `CodeEnforcer` is already installed locally, use `dotnet tool update CodeEnforcer --local --prerelease` instead of install.
