param(
  [string] $Configuration = 'Release',
  [string] $OutputPath = 'artifacts/packages'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$fullOutputPath = Join-Path $repoRoot $OutputPath
New-Item -ItemType Directory -Force -Path $fullOutputPath | Out-Null

$projects = @(
  'src/CodeEnforcer/CodeEnforcer.csproj',
  'src/CodeEnforcer.Analyzers/CodeEnforcer.Analyzers.csproj'
)

foreach ($project in $projects) {
  dotnet pack `
    (Join-Path $repoRoot $project) `
    --configuration $Configuration `
    --output $fullOutputPath

  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}
