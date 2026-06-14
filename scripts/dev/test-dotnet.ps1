# scripts/dev/test-dotnet.ps1 —— 对齐 test-dotnet.sh（支持 filter 参数）。
# 用法： .\test-dotnet.ps1 [filter]
param([string]$Filter = '')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\_common.ps1"

if (-not (Test-Cmd dotnet)) {
  Write-Host "dotnet is not installed. Install a .NET SDK that supports net10.0 first." -ForegroundColor Red
  exit 127
}

$projects = @(
  'tests/STOTOP.Module.Finance.Tests/STOTOP.Module.Finance.Tests.csproj',
  'tests/STOTOP.Module.CardFlow.Tests/STOTOP.Module.CardFlow.Tests.csproj',
  'tests/STOTOP.Module.Express.Tests/STOTOP.Module.Express.Tests.csproj'
)

$selected = @()
foreach ($p in $projects) {
  $name = [System.IO.Path]::GetFileNameWithoutExtension($p)
  if (-not $Filter -or $p -like "*$Filter*" -or $name -like "*$Filter*") { $selected += $p }
}
if ($selected.Count -eq 0) {
  if ($Filter) { Write-FailLine "no dotnet test projects selected for filter: $Filter" }
  else { Write-FailLine "no dotnet test projects selected" }
  exit 1
}

$passed = @(); $failures = @()
foreach ($p in $selected) {
  Write-Section $p
  dotnet test (Join-Path $RootDir $p) -m:1 /p:UseSharedCompilation=false
  if ($LASTEXITCODE -eq 0) { Write-Ok $p; $passed += $p }
  else { Write-FailLine "$p (exit $LASTEXITCODE)"; $failures += "$p (exit $LASTEXITCODE)" }
}

Write-Section "Dotnet test summary"
Write-Ok "$($passed.Count) passed"
if ($failures.Count -eq 0) { Write-Ok "all selected dotnet test projects passed" }
else {
  Write-FailLine "$($failures.Count) failed"
  foreach ($f in $failures) { Write-FailLine $f }
  exit 1
}
