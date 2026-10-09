param([switch]$SkipValidation)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot 'artifacts'
$publishRoot = Join-Path $outputRoot 'publish'
$buildRoot = Join-Path $outputRoot 'package-build'
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
Push-Location $projectRoot
try {
    # Keep package builds separate from development executables a user may have open.
    dotnet build KevlarEDR.slnx -c Release --artifacts-path $buildRoot --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if (-not $SkipValidation) {
        dotnet run --project tests/KevlarEDR.Validation -c Release --artifacts-path $buildRoot --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Recovery validation failed.' }
    }
    dotnet publish src/KevlarEDR.Desktop -c Release -r win-x64 --self-contained true --artifacts-path $buildRoot -o $publishRoot --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'src\KevlarEDR.Desktop\Assets\KevlarEDR.ico') -Destination $publishRoot
    # Remove two obsolete generated guide copies from an earlier package layout.
    foreach ($obsoleteGuide in @('installation.md','user-guide.md')) {
        $obsoletePath = Join-Path $publishRoot $obsoleteGuide
        if (Test-Path -LiteralPath $obsoletePath) { Remove-Item -LiteralPath $obsoletePath }
    }
    foreach ($documentName in @('README.md','CONTRIBUTING.md','CHANGELOG.md','SECURITY.md','THIRD_PARTY_NOTICES.md')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $documentName) -Destination $publishRoot
    }
    Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $publishRoot -Recurse -Force
    $noticesDirectory = Join-Path $publishRoot 'third-party'
    New-Item -ItemType Directory -Path $noticesDirectory -Force | Out-Null
    foreach ($upstreamProject in @('runtime','wpf')) {
        foreach ($noticeName in @('LICENSE.TXT','THIRD-PARTY-NOTICES.TXT')) {
            Invoke-WebRequest -Uri "https://raw.githubusercontent.com/dotnet/$upstreamProject/main/$noticeName" -OutFile (Join-Path $noticesDirectory "$upstreamProject-$noticeName") -UseBasicParsing
        }
    }
    $smokePath = Join-Path $outputRoot 'desktop-smoke.json'
    $smokeProcess = Start-Process -FilePath (Join-Path $publishRoot 'KevlarEDR.exe') -ArgumentList @('--smoke', ('"' + $smokePath + '"')) -WindowStyle Hidden -PassThru
    if (-not $smokeProcess.WaitForExit(45000)) { $smokeProcess.Kill(); throw 'Desktop smoke test timed out.' }
    if ($smokeProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $smokePath)) { throw 'Desktop smoke test failed.' }
    $archivePath = Join-Path $outputRoot 'KevlarEDR-0.1.0-win-x64-portable.zip'
    # Windows can briefly retain a just-executed binary during a scanner inspection.
    # Retry packaging without changing security settings or terminating scanners.
    for ($archiveAttempt = 1; $archiveAttempt -le 4; $archiveAttempt++) {
        try {
            Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $archivePath -Force -ErrorAction Stop
            break
        } catch {
            if ($archiveAttempt -eq 4) { throw }
            Write-Warning 'Portable packaging could not read a file; retrying shortly.'
            Start-Sleep -Seconds (2 * $archiveAttempt)
        }
    }
    $compilerCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )
    $compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $compiler) { throw 'Install Inno Setup 6 (winget install --id JRSoftware.InnoSetup --exact) and rerun.' }
    & $compiler ("/DSourceDir=$publishRoot") ("/DOutputDir=$outputRoot") (Join-Path $PSScriptRoot 'installer.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    $releaseFiles = @($archivePath, (Join-Path $outputRoot 'KevlarEDR-Setup-0.1.0-win-x64.exe'))
    $hashLines = $releaseFiles | ForEach-Object { $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256; "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))" }
    [IO.File]::WriteAllLines((Join-Path $outputRoot 'SHA256SUMS.txt'), $hashLines)
    Write-Output "Packages ready: $outputRoot"
} finally { Pop-Location }
