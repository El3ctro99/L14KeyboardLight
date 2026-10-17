param([switch]$MigratePrevious)
$ErrorActionPreference = 'Stop'
$sourceFile = Join-Path $PSScriptRoot 'bin\L14KeyboardLight.exe'
if (-not (Test-Path -LiteralPath $sourceFile)) { throw 'Előbb futtasd a build.ps1 fájlt, vagy töltsd le a bináris kiadást.' }
$stateDirectory = Join-Path $env:LOCALAPPDATA 'L14KeyboardLight'
$targetDirectory = Join-Path $env:LOCALAPPDATA 'Programs\L14KeyboardLight'
$targetFile = Join-Path $targetDirectory 'L14KeyboardLight.exe'
$currentSession = (Get-Process -Id $PID).SessionId
$legacyService = Get-Service -Name BacklightResetService -ErrorAction SilentlyContinue
$legacyRun = (Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name BacklightTrayApp -ErrorAction SilentlyContinue).BacklightTrayApp
$legacyShortcut = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Startup\Keyboard Backlight for Lenovo.lnk'
$legacyTray = @(Get-Process -Name BacklightTrayApp -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $currentSession })
$hasLegacyConflict = ($legacyService -and $legacyService.StartType -ne 'Disabled') -or $legacyRun -or (Test-Path -LiteralPath $legacyShortcut) -or $legacyTray.Count -gt 0
if ($hasLegacyConflict -and -not $MigratePrevious) {
    throw 'A korábbi Keyboard Backlight for Lenovo vezérlő aktív. Visszaállítható lecseréléshez használd az install.ps1 -MigratePrevious kapcsolót (egyszeri rendszergazdai jóváhagyás), vagy előbb állítsd le és kapcsold ki az automatikus indulását.'
}

function Invoke-LegacyTransition([switch]$Restore) {
    $transitionPath = Join-Path $PSScriptRoot 'legacy-transition.ps1'
    $transitionCode = "& {`n" + [IO.File]::ReadAllText($transitionPath) + "`n} -RootDirectory '" + $PSScriptRoot.Replace("'", "''") + "' -StateDirectory '" + $stateDirectory.Replace("'", "''") + "'"
    if ($Restore) { $transitionCode += ' -Restore' }
    $encodedCode = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($transitionCode))
    $transitionProcess = Start-Process -FilePath "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -Verb RunAs -ArgumentList @('-NoProfile', '-NonInteractive', '-EncodedCommand', $encodedCode) -WindowStyle Hidden -Wait -PassThru
    if ($transitionProcess.ExitCode -ne 0) { throw ('A régi vezérlő átállítása nem sikerült. Részletek: ' + (Join-Path $stateDirectory 'legacy-transition-result.txt')) }
}

New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
$preflightReport = Join-Path $stateDirectory 'install-preflight.txt'
$preflightProcess = Start-Process -FilePath $sourceFile -ArgumentList @('--status', ('"' + $preflightReport + '"')) -WindowStyle Hidden -Wait -PassThru
if ($preflightProcess.ExitCode -ne 0) { throw ('A laptop típusa vagy Lenovo-drivere nem támogatott. Lásd: ' + $preflightReport) }

$payloadFiles = @('README.md', 'README.hu.md', 'remove.ps1', 'legacy-transition.ps1')
$stagingDirectory = Join-Path $targetDirectory ('.install-' + [Guid]::NewGuid().ToString('N'))
$migrationCompleted = $false
$previousWasRunning = $false
try {
    # Stage every payload file before stopping or replacing any controller.
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
    Copy-Item -LiteralPath $sourceFile -Destination (Join-Path $stagingDirectory 'L14KeyboardLight.exe')
    foreach ($payloadFile in $payloadFiles) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $payloadFile) -Destination (Join-Path $stagingDirectory $payloadFile)
    }
    $previousProcesses = @(Get-Process -Name L14KeyboardLight -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $currentSession })
    $previousWasRunning = $previousProcesses.Count -gt 0
    if ($previousWasRunning) {
        Start-Process -FilePath $sourceFile -ArgumentList '--stop' -WindowStyle Hidden -Wait | Out-Null
        $deadline = [DateTime]::UtcNow.AddSeconds(8)
        do {
            $running = @(Get-Process -Name L14KeyboardLight -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $currentSession })
            if ($running.Count -eq 0) { break }
            Start-Sleep -Milliseconds 200
        } while ([DateTime]::UtcNow -lt $deadline)
        if ($running.Count -gt 0) { throw 'A futó L14 program nem lépett ki. Lépj ki a tálcamenüből, majd próbáld újra.' }
    }
    if ($MigratePrevious) { Invoke-LegacyTransition; $migrationCompleted = $true }
    foreach ($payloadFile in @('L14KeyboardLight.exe') + $payloadFiles) {
        Copy-Item -LiteralPath (Join-Path $stagingDirectory $payloadFile) -Destination (Join-Path $targetDirectory $payloadFile) -Force
    }
    $startupKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    if (-not (Test-Path -LiteralPath $startupKey)) { New-Item -Path $startupKey | Out-Null }
    Set-ItemProperty -LiteralPath $startupKey -Name L14KeyboardLight -Value ('"' + $targetFile + '"')
    $shellObject = New-Object -ComObject WScript.Shell
    $shortcutObject = $shellObject.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'L14 Keyboard Light.lnk'))
    $shortcutObject.TargetPath = $targetFile
    $shortcutObject.WorkingDirectory = $targetDirectory
    $shortcutObject.Description = 'L14 billentyűzetvilágítás automatikus vezérlése'
    $shortcutObject.Save()
    Start-Process -FilePath $targetFile -WindowStyle Hidden | Out-Null
    Write-Output ('Telepítve és elindítva: ' + $targetFile)
} catch {
    $installError = $_
    if ($migrationCompleted) {
        try { Invoke-LegacyTransition -Restore }
        catch { Write-Warning ('A korábbi vezérlő visszaállítása is hibát jelzett: ' + $_.Exception.Message) }
    }
    if ($previousWasRunning -and (Test-Path -LiteralPath $targetFile)) { Start-Process -FilePath $targetFile -WindowStyle Hidden | Out-Null }
    throw $installError
} finally {
    foreach ($payloadFile in @('L14KeyboardLight.exe') + $payloadFiles) {
        $stagedFile = Join-Path $stagingDirectory $payloadFile
        if (Test-Path -LiteralPath $stagedFile) { Remove-Item -LiteralPath $stagedFile }
    }
    if (Test-Path -LiteralPath $stagingDirectory) { Remove-Item -LiteralPath $stagingDirectory }
}
