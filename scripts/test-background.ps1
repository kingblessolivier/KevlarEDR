param([string]$Executable = (Join-Path $env:LOCALAPPDATA 'Programs\KevlarEDR\KevlarEDR.exe'))
$ErrorActionPreference = 'Stop'
$Executable = [IO.Path]::GetFullPath($Executable)
if (-not (Test-Path -LiteralPath $Executable)) { throw 'Installed executable is missing.' }
if (@(Get-Process KevlarEDR -ErrorAction SilentlyContinue).Count -ne 0) { throw 'Quit KevlarEDR before running the isolated background lifecycle check.' }
$projectRoot = Split-Path -Parent $PSScriptRoot
$runningApp = Start-Process -FilePath $Executable -ArgumentList '--background' -WindowStyle Hidden -PassThru
try {
    # Allow a cold self-contained launch to initialize before checking visibility.
    Start-Sleep -Seconds 2
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $runningApp.Refresh()
        if ($runningApp.HasExited -or $runningApp.MainWindowHandle -eq 0) { break }
        Start-Sleep -Milliseconds 200
    }
    if ($runningApp.HasExited -or $runningApp.MainWindowHandle -ne 0) { throw ('Sign-in mode failed: exited=' + $runningApp.HasExited + ', visibleWindow=' + $runningApp.MainWindowHandle) }
    $openRequest = Start-Process -FilePath $Executable -WindowStyle Hidden -PassThru
    if (-not $openRequest.WaitForExit(10000) -or $openRequest.ExitCode -ne 0) { throw 'Second launch did not reuse the background instance.' }
    Start-Sleep -Seconds 1
    $runningApp.Refresh()
    if ($runningApp.MainWindowHandle -eq 0 -or @(Get-Process KevlarEDR).Count -ne 1) { throw 'Existing window did not reopen as a single instance.' }
    if (-not $runningApp.CloseMainWindow()) { throw 'Could not exercise the window close action.' }
    Start-Sleep -Seconds 1
    $runningApp.Refresh()
    if ($runningApp.HasExited -or $runningApp.MainWindowHandle -ne 0) { throw 'Closing the window stopped background monitoring.' }
    $quitRequest = Start-Process -FilePath $Executable -ArgumentList '--exit' -WindowStyle Hidden -PassThru
    if (-not $quitRequest.WaitForExit(10000) -or -not $runningApp.WaitForExit(15000) -or $runningApp.ExitCode -ne 0) { throw 'Explicit Quit did not cleanly stop the background process.' }
    [IO.File]::WriteAllText((Join-Path $projectRoot 'artifacts\background-validation.json'), '{"HiddenSignInLaunchPassed":true,"SingleInstanceReopenPassed":true,"CloseKeepsMonitoringPassed":true,"ExplicitQuitPassed":true}')
    Write-Output 'PASS: hidden sign-in mode, single-instance reopen, close to notification area, explicit Quit.'
} finally {
    $runningApp.Refresh()
    if (-not $runningApp.HasExited) {
        $cleanupRequest = Start-Process -FilePath $Executable -ArgumentList '--exit' -WindowStyle Hidden -PassThru
        $cleanupRequest.WaitForExit(10000) | Out-Null
        $runningApp.WaitForExit(15000) | Out-Null
    }
}
