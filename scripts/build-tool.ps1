param(
  [string] $Configuration = 'Release',
  [string] $OutputPath = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$fullOutputPath = Join-Path $repoRoot $OutputPath
New-Item -ItemType Directory -Force -Path $fullOutputPath | Out-Null

dotnet pack `
  (Join-Path $repoRoot 'src/CodeEnforcer/CodeEnforcer.csproj') `
  --configuration $Configuration `
  --output $fullOutputPath
