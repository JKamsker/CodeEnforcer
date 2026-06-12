$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot

function Invoke-Checked {
  param(
    [string] $FilePath,
    [string[]] $Arguments
  )

  & $FilePath @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$FilePath exited with code $LASTEXITCODE."
  }
}

try {
  Invoke-Checked 'dotnet' @('restore', 'CodeEnforcer.slnx', '--locked-mode')
  Invoke-Checked 'dotnet' @('build', 'CodeEnforcer.slnx', '--configuration', 'Release', '--no-restore')
  Invoke-Checked 'dotnet' @('test', 'CodeEnforcer.slnx', '--configuration', 'Release', '--no-build')
  & ./scripts/check-csharp-file-lines.ps1
  if ($LASTEXITCODE -ne 0) {
    throw "CodeEnforcer self-check exited with code $LASTEXITCODE."
  }
}
finally {
  Pop-Location
}
