$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'A Windows .NET Framework 4.x C# fordítója nem található.' }
$binaryDirectory = Join-Path $sourceRoot 'bin'
New-Item -ItemType Directory -Path $binaryDirectory -Force | Out-Null
$sources = @('Program.cs', 'LenovoBacklightDevice.cs', 'Settings.cs', 'PowerWindow.cs', 'BacklightPolicy.cs', 'PolicyTests.cs', 'FullscreenDetector.cs', 'FullscreenTests.cs', 'UiText.cs', 'LocalizationTests.cs') | ForEach-Object { Join-Path $sourceRoot $_ }
& $compilerPath /nologo /langversion:5 /target:winexe /platform:x64 /optimize+ /warn:4 /warnaserror+ /codepage:65001 /utf8output "/win32manifest:$(Join-Path $sourceRoot 'app.manifest')" "/out:$(Join-Path $binaryDirectory 'L14KeyboardLight.exe')" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll $sources
if ($LASTEXITCODE -ne 0) { throw "Fordítási hiba: $LASTEXITCODE" }
Write-Output (Join-Path $binaryDirectory 'L14KeyboardLight.exe')
