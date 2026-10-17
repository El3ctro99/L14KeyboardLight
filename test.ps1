param([string]$OutputPath = (Join-Path $PSScriptRoot 'reports\self-test.txt'))
$ErrorActionPreference = 'Stop'
$testExecutable = Join-Path $PSScriptRoot 'bin\L14KeyboardLight.exe'
if (-not (Test-Path -LiteralPath $testExecutable)) { throw 'Build the application with build.ps1 first.' }
$absoluteReport = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($absoluteReport)) -Force | Out-Null
$testProcess = Start-Process -FilePath $testExecutable -ArgumentList @('--self-test', ('"' + $absoluteReport + '"')) -WindowStyle Hidden -Wait -PassThru
if (Test-Path -LiteralPath $absoluteReport) { Get-Content -LiteralPath $absoluteReport }
if ($testProcess.ExitCode -ne 0) { throw ('Tests failed: exit code ' + $testProcess.ExitCode) }
