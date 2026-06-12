param(
  [string] $Configuration = 'Release',
  [string] $OutputPath = 'artifacts/packages',
  [string] $Version = ''
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
  $packArguments = @(
    'pack',
    (Join-Path $repoRoot $project),
    '--configuration',
    $Configuration,
    '--output',
    $fullOutputPath
  )

  if ($Version.Length -gt 0) {
    $packArguments += "-p:Version=$Version"
  }

  dotnet @packArguments

  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}
