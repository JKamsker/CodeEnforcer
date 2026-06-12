$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot

try {
  dotnet restore CodeEnforcer.slnx --locked-mode
  dotnet build CodeEnforcer.slnx --configuration Release --no-restore
  dotnet test CodeEnforcer.slnx --configuration Release --no-build --logger 'trx;LogFileName=tests.trx'
  ./scripts/check-csharp-file-lines.ps1
}
finally {
  Pop-Location
}
