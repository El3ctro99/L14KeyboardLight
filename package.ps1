$ErrorActionPreference = 'Stop'
$sourceDirectory = $PSScriptRoot
$binaryFile = Join-Path $sourceDirectory 'bin\L14KeyboardLight.exe'
if (-not (Test-Path -LiteralPath $binaryFile)) { throw 'Run build.ps1 first.' }
$version = ([Version][Diagnostics.FileVersionInfo]::GetVersionInfo($binaryFile).FileVersion).ToString(3)
$outputDirectory = Join-Path $sourceDirectory 'dist'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$temporaryDirectory = Join-Path $outputDirectory ('package-' + [Guid]::NewGuid().ToString('N'))
$payloadDirectory = Join-Path $temporaryDirectory 'L14KeyboardLight'
$binaryDirectory = Join-Path $payloadDirectory 'bin'
$packageFiles = @('README.md', 'README.hu.md', 'CHANGELOG.md', 'VALIDATION.md', 'install.ps1', 'remove.ps1', 'legacy-transition.ps1')
try {
    New-Item -ItemType Directory -Path $binaryDirectory -Force | Out-Null
    Copy-Item -LiteralPath $binaryFile -Destination (Join-Path $binaryDirectory 'L14KeyboardLight.exe')
    foreach ($packageFile in $packageFiles) {
        Copy-Item -LiteralPath (Join-Path $sourceDirectory $packageFile) -Destination (Join-Path $payloadDirectory $packageFile)
    }
    $licenseFile = Join-Path $sourceDirectory 'LICENSE'
    if (Test-Path -LiteralPath $licenseFile) { Copy-Item -LiteralPath $licenseFile -Destination (Join-Path $payloadDirectory 'LICENSE') }
    $archiveFile = Join-Path $outputDirectory ('L14KeyboardLight-' + $version + '-win-x64.zip')
    Compress-Archive -LiteralPath $payloadDirectory -DestinationPath $archiveFile -Force
    $checksum = (Get-FileHash -LiteralPath $archiveFile -Algorithm SHA256).Hash.ToLowerInvariant()
    ($checksum + '  ' + [IO.Path]::GetFileName($archiveFile)) | Set-Content -LiteralPath ($archiveFile + '.sha256') -Encoding ASCII
    Write-Output $archiveFile
    Write-Output ($archiveFile + '.sha256')
} finally {
    # Only our explicit payload files and empty temporary directories are removed.
    $temporaryFiles = @('bin\L14KeyboardLight.exe', 'LICENSE') + $packageFiles
    foreach ($temporaryFile in $temporaryFiles) {
        $temporaryPath = Join-Path $payloadDirectory $temporaryFile
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath }
    }
    foreach ($temporaryFolder in @($binaryDirectory, $payloadDirectory, $temporaryDirectory)) {
        if (Test-Path -LiteralPath $temporaryFolder) { Remove-Item -LiteralPath $temporaryFolder }
    }
}
