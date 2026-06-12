param(
  [string] $Path = '.',
  [string] $PackageSource = 'https://api.nuget.org/v3/index.json',
  [string] $Version = '',
  [switch] $Stable,
  [switch] $Force
)

$ErrorActionPreference = 'Stop'

function Invoke-Checked {
  param(
    [string] $FilePath,
    [string[]] $Arguments,
    [string] $WorkingDirectory
  )

  Push-Location $WorkingDirectory
  try {
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
      throw "$FilePath exited with code $LASTEXITCODE in $WorkingDirectory."
    }
  }
  finally {
    Pop-Location
  }
}

function Get-CommandOutput {
  param(
    [string] $FilePath,
    [string[]] $Arguments,
    [string] $WorkingDirectory
  )

  Push-Location $WorkingDirectory
  try {
    $output = & $FilePath @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
      return $null
    }

    return ($output | Out-String).Trim()
  }
  finally {
    Pop-Location
  }
}

if (-not (Test-Path -LiteralPath $Path)) {
  New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

$targetPath = (Resolve-Path -LiteralPath $Path).Path
$repoRoot = Get-CommandOutput -FilePath 'git' -Arguments @('-C', $targetPath, 'rev-parse', '--show-toplevel') -WorkingDirectory $targetPath
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
  Invoke-Checked -FilePath 'git' -Arguments @('-C', $targetPath, 'init') -WorkingDirectory $targetPath
  $repoRoot = Get-CommandOutput -FilePath 'git' -Arguments @('-C', $targetPath, 'rev-parse', '--show-toplevel') -WorkingDirectory $targetPath
}

if ([string]::IsNullOrWhiteSpace($repoRoot)) {
  throw "Could not resolve repository root for $targetPath."
}

$manifestPaths = @(
  (Join-Path $repoRoot '.config/dotnet-tools.json'),
  (Join-Path $repoRoot 'dotnet-tools.json')
)
$hasManifest = $false
foreach ($manifestPath in $manifestPaths) {
  if (Test-Path -LiteralPath $manifestPath) {
    $hasManifest = $true
    break
  }
}

if (-not $hasManifest) {
  Invoke-Checked -FilePath 'dotnet' -Arguments @('new', 'tool-manifest') -WorkingDirectory $repoRoot
}

$toolList = Get-CommandOutput -FilePath 'dotnet' -Arguments @('tool', 'list', '--local') -WorkingDirectory $repoRoot
$isInstalled = $toolList -match '(^|\s)codeenforcer(\s|$)'
$toolCommand = if ($isInstalled) { 'update' } else { 'install' }
$toolArgs = @('tool', $toolCommand, 'CodeEnforcer', '--local', '--add-source', $PackageSource)

if ($Version.Length -gt 0) {
  $toolArgs += @('--version', $Version)
}
elseif (-not $Stable) {
  $toolArgs += '--prerelease'
}

Invoke-Checked -FilePath 'dotnet' -Arguments $toolArgs -WorkingDirectory $repoRoot

$initArgs = @('tool', 'run', 'code-enforcer', '--', 'init', '--root', $repoRoot)
if ($Force) {
  $initArgs += '--force'
}

Invoke-Checked -FilePath 'dotnet' -Arguments $initArgs -WorkingDirectory $repoRoot

$hooksPath = Get-CommandOutput -FilePath 'git' -Arguments @('-C', $repoRoot, 'config', 'core.hooksPath') -WorkingDirectory $repoRoot
if ($hooksPath -ne '.githooks') {
  throw "Expected core.hooksPath to be .githooks, but was '$hooksPath'."
}

$configPath = Join-Path $repoRoot '.config/code-enforcer/code-enforcer.json'
Invoke-Checked -FilePath 'dotnet' -Arguments @('tool', 'run', 'code-enforcer', '--', 'check', '--root', $repoRoot, '--config', $configPath) -WorkingDirectory $repoRoot

Write-Host "Initialized CodeEnforcer in $repoRoot"
