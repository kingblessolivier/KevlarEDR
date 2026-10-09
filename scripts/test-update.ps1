# Apply the locally built update to the default per-user installation and verify
# both the new UI and existing recovery metadata. Run after scripts/package.ps1.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$outputRoot = Join-Path $projectRoot 'artifacts'
$installedRoot = Join-Path $env:LOCALAPPDATA 'Programs\KevlarEDR'
$installedExecutable = Join-Path $installedRoot 'KevlarEDR.exe'
$installKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{E7872FF6-80D4-4B66-BF63-540538DA870A}_is1'
if (Test-Path -LiteralPath $installKey) {
    $existingLocation = (Get-ItemProperty -LiteralPath $installKey).InstallLocation
    if ($existingLocation -and [IO.Path]::GetFullPath($existingLocation).TrimEnd('\') -ne $installedRoot.TrimEnd('\')) {
        throw 'Existing app is installed in another location; this check only updates the default per-user installation.'
    }
}
foreach ($appProcess in @(Get-Process KevlarEDR -ErrorAction SilentlyContinue)) {
    $appPath = $appProcess.Path
    if ($appPath -eq $installedExecutable -or ($appPath -and $appPath.StartsWith($projectRoot + '\', [StringComparison]::OrdinalIgnoreCase))) {
        # Older builds exit on Close; current builds hide and need the explicit tray quit command.
        $windowClosed = $appProcess.CloseMainWindow()
        if (-not $windowClosed -or -not $appProcess.WaitForExit(3000)) {
            $exitRequest = Start-Process -FilePath $appPath -ArgumentList '--exit' -WindowStyle Hidden -PassThru
            if (-not $exitRequest.WaitForExit(10000) -or -not $appProcess.WaitForExit(15000)) { throw 'An app did not quit gracefully; update stopped.' }
        }
    }
}
$dataRoot = Join-Path $env:LOCALAPPDATA 'KevlarEDR'
$statePath = Join-Path $dataRoot 'state.json'
$stateBefore = if (Test-Path -LiteralPath $statePath) { (Get-FileHash -LiteralPath $statePath -Algorithm SHA256).Hash } else { $null }
New-Item -ItemType Directory -Path $dataRoot -Force | Out-Null
$marker = Join-Path $dataRoot ('update-retention-' + [guid]::NewGuid().ToString('N') + '.txt')
[IO.File]::WriteAllText($marker, 'Disposable update retention fixture')
try {
    $setup = Start-Process -FilePath (Join-Path $outputRoot 'KevlarEDR-Setup-0.1.0-win-x64.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/TASKS="desktopicon,autostart"',('/DIR="' + $installedRoot + '"')) -WindowStyle Hidden -PassThru
    if (-not $setup.WaitForExit(60000) -or $setup.ExitCode -ne 0) { throw 'Update installation failed.' }
    if ((Get-FileHash -LiteralPath $installedExecutable -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath (Join-Path $outputRoot 'publish\KevlarEDR.exe') -Algorithm SHA256).Hash) { throw 'Installed executable differs from the tested published build.' }
    $report = Join-Path $outputRoot 'updated-desktop-smoke.json'
    $app = Start-Process -FilePath $installedExecutable -ArgumentList @('--smoke',('"' + $report + '"')) -WindowStyle Hidden -PassThru
    if (-not $app.WaitForExit(45000) -or $app.ExitCode -ne 0) { throw 'Updated desktop smoke failed.' }
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if (-not $result.FileBrowsingPassed -or -not $result.DeletedFileRestored -or -not $result.DeviceArrivalRemovalSimulationPassed -or $result.RenderedPages.Count -ne 7) { throw 'Updated UI/recovery/device report is incomplete.' }
    $shell = New-Object -ComObject WScript.Shell
    $shortcutPath = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'KevlarEDR.lnk'
    if (-not (Test-Path -LiteralPath $shortcutPath) -or $shell.CreateShortcut($shortcutPath).TargetPath -ne $installedExecutable) { throw 'Installed desktop shortcut is missing or points to another app.' }
    $startup = (Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run').KevlarEDR
    if ($startup -ne ('"' + $installedExecutable + '" --background')) { throw 'Installed sign-in command is incorrect.' }
    $stateAfter = if (Test-Path -LiteralPath $statePath) { (Get-FileHash -LiteralPath $statePath -Algorithm SHA256).Hash } else { $null }
    if ($stateBefore -ne $stateAfter -or -not (Test-Path -LiteralPath $marker)) { throw 'Update changed saved recovery data.' }
    [IO.File]::WriteAllText((Join-Path $outputRoot 'update-validation.json'), '{"InstalledBuildMatches":true,"UpdatedAppSmokePassed":true,"DeviceSimulationPassed":true,"DesktopShortcutVerified":true,"SignInStartupVerified":true,"SavedMetadataUnchanged":true,"RetentionMarkerPreserved":true}')
    Write-Output 'PASS: per-user update, installed Explorer/recovery smoke, and unchanged saved recovery metadata.'
} finally { if (Test-Path -LiteralPath $marker) { Remove-Item -LiteralPath $marker } }
