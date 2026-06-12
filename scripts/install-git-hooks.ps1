$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
git -C $repoRoot config core.hooksPath .githooks
Write-Host 'Configured git hooks path to .githooks.'
