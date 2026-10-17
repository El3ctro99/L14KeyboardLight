param(
    [switch]$Restore,
    [string]$RootDirectory = $PSScriptRoot,
    [string]$StateDirectory = (Join-Path $env:LOCALAPPDATA 'L14KeyboardLight')
)
$ErrorActionPreference = 'Stop'
$backupDirectory = Join-Path $StateDirectory 'legacy-backup'
$backupFile = Join-Path $backupDirectory 'state.json'
$resultFile = Join-Path $StateDirectory 'legacy-transition-result.txt'
$runPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
$legacyDirectory = Join-Path $env:ProgramFiles 'KeyboardBacklightForLenovo'
$expectedService = Join-Path $legacyDirectory 'BacklightResetService.exe'
$expectedTray = Join-Path $legacyDirectory 'BacklightTrayApp.exe'
$shortcutPath = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Startup\Keyboard Backlight for Lenovo.lnk'
$disabledShortcut = $shortcutPath + '.disabled-by-L14KeyboardLight'
$backupShortcut = Join-Path $backupDirectory 'Keyboard Backlight for Lenovo.lnk'
$journal = New-Object 'System.Collections.Generic.List[object]'
$conflicts = New-Object 'System.Collections.Generic.List[string]'

function Test-ExactExecutable([string]$Command, [string]$Expected) {
    if ([string]::IsNullOrWhiteSpace($Command)) { return $false }
    $candidate = $Command.Trim()
    if ($candidate.StartsWith('"')) {
        if ($candidate.Length -lt 2 -or -not $candidate.EndsWith('"')) { return $false }
        $candidate = $candidate.Substring(1, $candidate.Length - 2)
    }
    if ($candidate.Contains('"')) { return $false }
    try {
        $resolved = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($candidate))
        return [string]::Equals($resolved, [IO.Path]::GetFullPath($Expected), [StringComparison]::OrdinalIgnoreCase)
    } catch { return $false }
}

function Get-KnownService {
    $service = Get-CimInstance Win32_Service -Filter "Name='BacklightResetService'"
    if ($service -and -not (Test-ExactExecutable $service.PathName $expectedService)) {
        throw 'A BacklightResetService mas programhoz tartozik; nem modosithato.'
    }
    return $service
}

function Convert-StartupMode([string]$Mode) {
    switch ($Mode) {
        'Auto' { return 'Automatic' }
        'Automatic' { return 'Automatic' }
        'Manual' { return 'Manual' }
        'Disabled' { return 'Disabled' }
        default { throw ('Ismeretlen szolgaltatas-inditasi mod: ' + $Mode) }
    }
}

function Confirm-KnownService {
    $service = Get-KnownService
    if (-not $service) { throw 'A korabbi BacklightResetService mar nem talalhato.' }
    return $service
}

function Set-KnownServiceState([string]$Mode, [bool]$Running) {
    $startup = Convert-StartupMode $Mode
    $service = Confirm-KnownService
    if ($service.State -ne 'Running' -and $service.State -ne 'Stopped') {
        throw 'A BacklightResetService allapotvaltasa meg folyamatban van.'
    }
    if (-not $Running -and $service.State -eq 'Running') {
        $null = Confirm-KnownService
        Stop-Service -Name BacklightResetService
    }
    if ($Running -and $service.State -ne 'Running') {
        # A previously running but disabled service needs a temporary start mode.
        $null = Confirm-KnownService
        Set-Service -Name BacklightResetService -StartupType Manual
        $null = Confirm-KnownService
        Start-Service -Name BacklightResetService
    }
    $null = Confirm-KnownService
    Set-Service -Name BacklightResetService -StartupType $startup
}

function Get-RunValue {
    return (Get-ItemProperty -LiteralPath $runPath -Name BacklightTrayApp -ErrorAction SilentlyContinue).BacklightTrayApp
}

function Test-SameFile([string]$First, [string]$Second) {
    return (Get-FileHash -LiteralPath $First -Algorithm SHA256).Hash -eq (Get-FileHash -LiteralPath $Second -Algorithm SHA256).Hash
}

function Undo-Changes {
    for ($index = $journal.Count - 1; $index -ge 0; $index--) {
        $change = $journal[$index]
        try {
            switch ($change.Kind) {
                'Service' { Set-KnownServiceState $change.StartMode $change.Running }
                'RunRemoved' {
                    $current = Get-RunValue
                    if ($null -eq $current) { Set-ItemProperty -LiteralPath $runPath -Name BacklightTrayApp -Value $change.Value }
                    elseif ($current -ne $change.Value) { throw 'A Run ertek idokozben megvaltozott; erintetlen maradt.' }
                }
                'RunRestored' {
                    $current = Get-RunValue
                    if ($current -eq $change.Value) { Remove-ItemProperty -LiteralPath $runPath -Name BacklightTrayApp }
                    elseif ($null -ne $current) { throw 'A Run ertek idokozben megvaltozott; erintetlen maradt.' }
                }
                'ShortcutRemoved' {
                    if (-not (Test-Path -LiteralPath $change.Path)) { Copy-Item -LiteralPath $backupShortcut -Destination $change.Path }
                    elseif (-not (Test-SameFile $change.Path $backupShortcut)) { throw 'Az inditasi parancsikon idokozben megvaltozott; erintetlen maradt.' }
                }
                'ShortcutRestored' {
                    if (Test-Path -LiteralPath $change.Path) {
                        if (Test-SameFile $change.Path $backupShortcut) { Remove-Item -LiteralPath $change.Path }
                        else { throw 'Az inditasi parancsikon idokozben megvaltozott; erintetlen maradt.' }
                    }
                }
            }
        } catch { $conflicts.Add('ROLLBACK: ' + $_.Exception.Message) }
    }
}

try {
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
    # Import only the two named files from the original local installation.
    $localBackup = Join-Path $RootDirectory 'legacy-backup'
    $localState = Join-Path $localBackup 'state.json'
    if (-not (Test-Path -LiteralPath $backupFile) -and (Test-Path -LiteralPath $localState)) {
        Copy-Item -LiteralPath $localState -Destination $backupFile
        $localShortcut = Join-Path $localBackup 'Keyboard Backlight for Lenovo.lnk'
        if ((Test-Path -LiteralPath $localShortcut) -and -not (Test-Path -LiteralPath $backupShortcut)) {
            Copy-Item -LiteralPath $localShortcut -Destination $backupShortcut
        }
    }
    $service = Get-KnownService
    $runValue = Get-RunValue

    if ($Restore) {
        if (-not (Test-Path -LiteralPath $backupFile)) { throw 'Nincs mentes a korabbi vezerlorol.' }
        $saved = Get-Content -LiteralPath $backupFile -Raw | ConvertFrom-Json
        if ($null -ne $saved.RunValue -and -not (Test-ExactExecutable $saved.RunValue $expectedTray)) {
            throw 'A mentett Run ertek nem a vart Lenovo vezerlore mutat; nem allithato vissza.'
        }
        if ($saved.ServicePresent) { $null = Convert-StartupMode $saved.StartMode }
        # Recover the suffix used by the first local installer, outside Startup.
        if (Test-Path -LiteralPath $disabledShortcut) {
            if ((Test-Path -LiteralPath $backupShortcut) -and -not (Test-SameFile $disabledShortcut $backupShortcut)) {
                $conflicts.Add('A korabbi letiltott parancsikon elter a mentestol; erintetlen maradt.')
            } else {
                $journal.Add(@{ Kind = 'ShortcutRemoved'; Path = $disabledShortcut })
                if (Test-Path -LiteralPath $backupShortcut) { Remove-Item -LiteralPath $disabledShortcut }
                else { Move-Item -LiteralPath $disabledShortcut -Destination $backupShortcut }
            }
        }
        if ($null -ne $saved.RunValue) {
            if ($null -eq $runValue) {
                if ($null -ne (Get-RunValue)) { throw 'A Run ertek idokozben megvaltozott; nem irhato felul.' }
                $journal.Add(@{ Kind = 'RunRestored'; Value = $saved.RunValue })
                Set-ItemProperty -LiteralPath $runPath -Name BacklightTrayApp -Value $saved.RunValue
            } elseif (-not (Test-ExactExecutable $runValue $expectedTray)) {
                $conflicts.Add('A BacklightTrayApp Run ertek megvaltozott; erintetlen maradt.')
            }
        }
        if ($saved.ShortcutPresent) {
            if (-not (Test-Path -LiteralPath $backupShortcut)) { $conflicts.Add('A mentett inditasi parancsikon hianyzik.') }
            elseif (-not (Test-Path -LiteralPath $shortcutPath)) {
                $journal.Add(@{ Kind = 'ShortcutRestored'; Path = $shortcutPath })
                Copy-Item -LiteralPath $backupShortcut -Destination $shortcutPath
            } elseif (-not (Test-SameFile $shortcutPath $backupShortcut)) {
                $conflicts.Add('Az inditasi parancsikon megvaltozott; erintetlen maradt.')
            }
        }
        if ($saved.ServicePresent) {
            if (-not $service) { $conflicts.Add('A korabbi BacklightResetService mar nem talalhato.') }
            else {
                $alreadyOriginal = ((Convert-StartupMode $service.StartMode) -eq (Convert-StartupMode $saved.StartMode)) -and (($service.State -eq 'Running') -eq [bool]$saved.ServiceRunning)
                $stillDisabled = $service.StartMode -eq 'Disabled' -and $service.State -eq 'Stopped'
                if (-not $alreadyOriginal -and -not $stillDisabled) {
                    $conflicts.Add('A BacklightResetService allapota megvaltozott; erintetlen maradt.')
                } elseif (-not $alreadyOriginal) {
                    $journal.Add(@{ Kind = 'Service'; StartMode = $service.StartMode; Running = ($service.State -eq 'Running') })
                    Set-KnownServiceState $saved.StartMode ([bool]$saved.ServiceRunning)
                }
            }
        }
        if ($conflicts.Count -gt 0) {
            # The caller retains the new controller on failure, so do not leave
            # any independent legacy items restored alongside that controller.
            Undo-Changes
            ('RESTORE ABORTED: conflicting changes preserved; attempted changes rolled back.' + [Environment]::NewLine + ($conflicts -join [Environment]::NewLine)) | Set-Content -LiteralPath $resultFile -Encoding UTF8
            exit 1
        }
        'RESTORED' | Set-Content -LiteralPath $resultFile -Encoding UTF8
    } else {
        if ($null -ne $runValue -and -not (Test-ExactExecutable $runValue $expectedTray)) {
            throw 'A BacklightTrayApp Run ertek mas programra mutat; erintetlen maradt.'
        }
        if (-not (Test-Path -LiteralPath $backupFile)) {
            [ordered]@{
                ServicePresent = ($null -ne $service)
                StartMode = $service.StartMode
                ServiceRunning = ($service.State -eq 'Running')
                RunValue = $runValue
                ShortcutPresent = ((Test-Path -LiteralPath $shortcutPath) -or (Test-Path -LiteralPath $disabledShortcut))
            } | ConvertTo-Json | Set-Content -LiteralPath $backupFile -Encoding UTF8
        }
        # Never replace an existing backup with a different, later shortcut.
        foreach ($candidate in @($shortcutPath, $disabledShortcut)) {
            if ((Test-Path -LiteralPath $candidate) -and (Test-Path -LiteralPath $backupShortcut) -and -not (Test-SameFile $candidate $backupShortcut)) {
                throw ('Az inditasi parancsikon elter a mentestol; erintetlen maradt: ' + $candidate)
            }
        }
        if ((Test-Path -LiteralPath $shortcutPath) -and (Test-Path -LiteralPath $disabledShortcut) -and -not (Test-SameFile $shortcutPath $disabledShortcut)) {
            throw 'A ket korabbi inditasi parancsikon elter; erintetlenek maradtak.'
        }
        if ($service) {
            $journal.Add(@{ Kind = 'Service'; StartMode = $service.StartMode; Running = ($service.State -eq 'Running') })
            Set-KnownServiceState 'Disabled' $false
        }
        if ($null -ne $runValue) {
            if ((Get-RunValue) -ne $runValue) { throw 'A Run ertek idokozben megvaltozott; nem torolheto.' }
            $journal.Add(@{ Kind = 'RunRemoved'; Value = $runValue })
            Remove-ItemProperty -LiteralPath $runPath -Name BacklightTrayApp
        }
        foreach ($candidate in @($shortcutPath, $disabledShortcut)) {
            if (Test-Path -LiteralPath $candidate) {
                $journal.Add(@{ Kind = 'ShortcutRemoved'; Path = $candidate })
                if (Test-Path -LiteralPath $backupShortcut) {
                    if (-not (Test-SameFile $candidate $backupShortcut)) { throw 'A parancsikon idokozben megvaltozott; nem torolheto.' }
                    Remove-Item -LiteralPath $candidate
                } else { Move-Item -LiteralPath $candidate -Destination $backupShortcut }
            }
        }
        $legacyCli = Join-Path $legacyDirectory 'keyboard-backlight.exe'
        if (Test-Path -LiteralPath $legacyCli) {
            & $legacyCli close-tray | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'A korabbi talcaprogram nem allt le szabalyosan.' }
        }
        'DISABLED' | Set-Content -LiteralPath $resultFile -Encoding UTF8
    }
    exit 0
} catch {
    $originalError = $_.Exception.Message
    if ($journal.Count -gt 0) { Undo-Changes }
    try {
        ('ERROR: ' + $originalError + [Environment]::NewLine + ($conflicts -join [Environment]::NewLine)) | Set-Content -LiteralPath $resultFile -Encoding UTF8
    } catch { Write-Error ('Nem irhato a migracios eredmeny: ' + $originalError) }
    exit 1
}
