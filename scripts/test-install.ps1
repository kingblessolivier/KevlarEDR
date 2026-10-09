$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts'))
$existingInstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{E7872FF6-80D4-4B66-BF63-540538DA870A}_is1'
if (Test-Path -LiteralPath $existingInstallKey) { throw 'Run installer validation on a clean test account/VM; an existing KevlarEDR installation was found.' }
$testDirectory = [IO.Path]::GetFullPath((Join-Path $artifactsRoot ('install-test-' + [guid]::NewGuid().ToString('N'))))
if (-not $testDirectory.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid test installation path.' }
$installer = Join-Path $artifactsRoot 'KevlarEDR-Setup-0.1.0-win-x64.exe'
$logPath = Join-Path $artifactsRoot 'installer-test.log'
$install = Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/DIR="' + $testDirectory + '"'),('/LOG="' + $logPath + '"')) -WindowStyle Hidden -PassThru
if (-not $install.WaitForExit(60000) -or $install.ExitCode -ne 0) { throw 'Installation failed or timed out.' }
$executable = Join-Path $testDirectory 'KevlarEDR.exe'
if (-not (Test-Path -LiteralPath $executable)) { throw 'Installed executable missing.' }
$smokeReport = Join-Path $artifactsRoot 'installed-desktop-smoke.json'
$app = Start-Process -FilePath $executable -ArgumentList @('--smoke',('"' + $smokeReport + '"')) -WindowStyle Hidden -PassThru
if (-not $app.WaitForExit(45000) -or $app.ExitCode -ne 0) { throw 'Installed desktop smoke test failed.' }
$dataDirectory = Join-Path $env:LOCALAPPDATA 'KevlarEDR'
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
$marker = Join-Path $dataDirectory ('installer-retention-test-' + [guid]::NewGuid().ToString('N') + '.txt')
[IO.File]::WriteAllText($marker, 'Disposable installer retention fixture')
$uninstaller = Join-Path $testDirectory 'unins000.exe'
$uninstall = Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru
if (-not $uninstall.WaitForExit(60000) -or $uninstall.ExitCode -ne 0) { throw 'Uninstall failed or timed out.' }
if (Test-Path -LiteralPath $executable) { throw 'Uninstall did not remove the app.' }
if (-not (Test-Path -LiteralPath $marker)) { throw 'Uninstall removed recovery data.' }
Remove-Item -LiteralPath $marker
[IO.File]::WriteAllText((Join-Path $artifactsRoot 'installer-validation.json'), '{"Installed":true,"InstalledAppSmokePassed":true,"Uninstalled":true,"RecoveryDataRetained":true}')
Write-Output 'PASS: installation, installed recovery smoke, uninstall, and recovery-data retention.'
