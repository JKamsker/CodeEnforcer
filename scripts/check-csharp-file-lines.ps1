$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
$project = Join-Path $repoRoot 'src/CodeEnforcer/CodeEnforcer.csproj'
$config = Join-Path $repoRoot '.config/code-enforcer/code-enforcer.json'

dotnet run --project $project --configuration Release -- --root $repoRoot --config $config
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}
