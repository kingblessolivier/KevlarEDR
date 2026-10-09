# Validation evidence — 0.1.0

Date: 2026-10-09. Local environment: Windows 11 Pro x64, build 26200;
.NET SDK 10.0.401; Inno Setup compiler 6.7.3.

## Completed locally

`dotnet build KevlarEDR.slnx -c Release`: succeeded with zero warnings/errors.

`dotnet run --project tests/KevlarEDR.Validation -c Release`: 31 scenarios passed.
Scenarios cover overwrite/delete and byte-for-byte recovery, destination collisions,
metadata reopening, same-size blob corruption, full vault, file/record limits,
locked files, concurrent capture, separate-process locking/cancellation, observer
capture and removed-folder events, linked paths, malformed/unsupported metadata,
binary streaming, missing scan targets, and persistent failed-scan status.

Real Defender adapter smoke against a harmless text fixture: exit 0; provider
reported no threats. This establishes scan integration for that fixture, not
malware-detection coverage or a guarantee about other files.

Published executable native smoke: window opened; Overview, Scans, Devices,
Recovery, Activity, and Settings rendered. The real desktop process captured a
baseline, changed/deleted its disposable document, and restored its original.

Installer: per-user setup succeeded in a disposable installation directory;
installed executable passed the same native recovery smoke. Uninstaller removed
the app while leaving a deliberately created recovery-data marker intact.

## Repeatable commands

```powershell
dotnet build KevlarEDR.slnx -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release -- --scan-smoke artifacts/defender-smoke.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1
# Run this only on a clean test account/VM without an existing installation.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-install.ps1
```

Local evidence files stay under ignored `artifacts/`; CI uploads sanitized test
artifacts separately. Do not commit personal paths, scanner logs, or vault data.

## Limits of this evidence

The UI smoke renders all pages and exercises recovery operations internally; it
does not establish a full manual accessibility/usability assessment or every file
dialog interaction. No drivers, malicious payloads, hardware USB enforcement,
company remote control, or production load benchmark were tested. Those features
are not implemented in 0.1.

Windows CI repeats build, core simulations, desktop startup/recovery, packaging,
installation, and data-retention checks. Check the PR/release run for its actual
outcome rather than treating a workflow file as evidence of execution.
