param([switch]$RestorePrevious)
$ErrorActionPreference = 'Stop'
$targetDirectory = Join-Path $env:LOCALAPPDATA 'Programs\L14KeyboardLight'
$stateDirectory = Join-Path $env:LOCALAPPDATA 'L14KeyboardLight'
$targetFile = Join-Path $targetDirectory 'L14KeyboardLight.exe'
$currentSession = (Get-Process -Id $PID).SessionId
$previousWasRunning = @(Get-Process -Name L14KeyboardLight -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $currentSession }).Count -gt 0
if (Test-Path -LiteralPath $targetFile) {
    Start-Process -FilePath $targetFile -ArgumentList '--stop' -WindowStyle Hidden -Wait | Out-Null
    $deadline = [DateTime]::UtcNow.AddSeconds(8)
    do {
        $running = @(Get-Process -Name L14KeyboardLight -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $currentSession })
        if ($running.Count -eq 0) { break }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($running.Count -gt 0) { throw 'A program nem lépett ki; a fájlokat nem töröltem.' }
}
if ($RestorePrevious) {
    try {
        $transition = Join-Path $PSScriptRoot 'legacy-transition.ps1'
        $transitionCode = "& {`n" + [IO.File]::ReadAllText($transition) + "`n} -Restore -RootDirectory '" + $PSScriptRoot.Replace("'", "''") + "' -StateDirectory '" + $stateDirectory.Replace("'", "''") + "'"
        $encodedCode = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($transitionCode))
        $restoreProcess = Start-Process -FilePath "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -Verb RunAs -ArgumentList @('-NoProfile', '-NonInteractive', '-EncodedCommand', $encodedCode) -WindowStyle Hidden -Wait -PassThru
        if ($restoreProcess.ExitCode -ne 0) { throw ('A régi program visszaállítása nem sikerült. Lásd: ' + (Join-Path $stateDirectory 'legacy-transition-result.txt')) }
    } catch {
        if ($previousWasRunning -and (Test-Path -LiteralPath $targetFile)) { Start-Process -FilePath $targetFile -WindowStyle Hidden | Out-Null }
        throw
    }
}
Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name L14KeyboardLight -ErrorAction SilentlyContinue
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'L14 Keyboard Light.lnk'
if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
# Remove only the known installed files. Keep preferences and persistent rollback data.
foreach ($fileName in @('L14KeyboardLight.exe', 'README.md', 'README.hu.md', 'remove.ps1', 'legacy-transition.ps1')) {
    $installedFile = Join-Path $targetDirectory $fileName
    if (Test-Path -LiteralPath $installedFile) { Remove-Item -LiteralPath $installedFile }
}
if ($RestorePrevious) {
    $legacyTray = Join-Path $env:ProgramFiles 'KeyboardBacklightForLenovo\BacklightTrayApp.exe'
    $savedStateFile = Join-Path $stateDirectory 'legacy-backup\state.json'
    $wasEnabled = $false
    if (Test-Path -LiteralPath $savedStateFile) {
        $savedState = Get-Content -LiteralPath $savedStateFile -Raw | ConvertFrom-Json
        $wasEnabled = $savedState.ServiceRunning -or $savedState.RunValue -or $savedState.ShortcutPresent
    }
    if ($wasEnabled -and (Test-Path -LiteralPath $legacyTray)) { Start-Process -FilePath $legacyTray -WindowStyle Hidden | Out-Null }
}
Write-Output 'Az L14 Keyboard Light eltávolítva. A beállítások és a visszaállításhoz szükséges mentés megmaradtak.'
