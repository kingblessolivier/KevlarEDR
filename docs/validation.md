# Validation evidence — 0.1.0

Date: 2026-10-09. Local environment: Windows 11 Pro x64, build 26200;
.NET SDK 10.0.401; Inno Setup compiler 6.7.3.

## Completed locally

`dotnet build KevlarEDR.slnx -c Release`: succeeded with zero warnings/errors.

`dotnet run --project tests/KevlarEDR.Validation -c Release`: 53 scenarios passed.
Scenarios cover overwrite/delete and byte-for-byte recovery, destination collisions,
metadata reopening, same-size blob corruption, full vault, file/record limits,
locked files, concurrent capture, separate-process locking/cancellation, observer
capture and removed-folder events, linked paths, malformed/unsupported metadata,
binary streaming, missing scan targets, and persistent failed-scan status.
Browser cases cover directories first, non-recursive listing, hidden-item handling,
real file size, unchanged source content, bounded/truncated listings, missing folders,
local-path validation, cancellation, and linked-location identification/rejection.
Volume scenarios cover a silent startup baseline, connection/removal deduplication,
delayed readiness, unavailable media, excluded network volumes and Fixed external disks.
Native desktop smoke sends a synthetic arrival message only to its own HWND, injects
test inventories, verifies banner/activity updates, inventory-error handling and safe
navigation after removal. This is simulated; no physical USB insertion/removal was
performed by the agent.

Real Defender adapter smoke against a harmless text fixture: exit 0; provider
reported no threats. This establishes scan integration for that fixture, not
malware-detection coverage or a guarantee about other files.

Published executable native smoke: window opened; This PC, Scans, Devices,
Recovery, Activity, Settings and System rendered. The real desktop process captured a
baseline, changed/deleted its disposable document, and restored its original.
The redesigned UI smoke also verifies 14 installed Windows icon glyphs, recovery
and activity search filters, saved-version selection identity, and minimum-window
rendering. Explorer checks include actual file-type icons, file metadata/filtering,
Details/Icons views, back/forward/up navigation, retained location after failed
navigation, and unchanged source content. Screenshots were visually inspected; no missing glyphs
or overlapping main controls were observed.

Installer: per-user setup succeeded in a disposable installation directory;
installed executable passed the same native recovery smoke. Uninstaller removed
the app while leaving a deliberately created recovery-data marker intact.
The filesystem UI update was also applied to the default per-user installation:
the installed binary matches the published build, the seven-section/browser/recovery
smoke passes, and existing recovery metadata plus a retention marker remain intact.

Background lifecycle check: hidden sign-in launch, second-launch reopening of the
same process, window close retaining that process, and explicit Quit all passed.
The update check additionally verifies the desktop shortcut target and per-user
sign-in command. These checks use the normal Windows executable, not a web mockup.

## Repeatable commands

```powershell
dotnet build KevlarEDR.slnx -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release -- --scan-smoke artifacts/defender-smoke.json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1
# Run this only on a clean test account/VM without an existing installation.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-install.ps1
# Apply/test a build on the default per-user installation (quits KevlarEDR first).
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-update.ps1
# Quit the app first; exercises the installed background process and returns it to stopped.
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-background.ps1
```

Local evidence stays under ignored `artifacts/`. CI is configured to upload
disposable-fixture evidence; screenshots include the runner hostname and temporary
paths. Do not commit personal scanner logs, documents, or vault data.

## Hosted CI status

PR #1's Windows job did not start. GitHub's check reported: "The job was not
started because your account is locked due to a billing issue."
This is not a successful CI run. Main's required `validate` check remains enabled;
merge and tagged publication await restored GitHub Actions availability.

## Limits of this evidence

The UI smoke renders all pages and exercises recovery operations internally; it
does not establish a full manual accessibility/usability assessment or every file
dialog interaction. No drivers, malicious payloads, hardware USB enforcement,
company remote control, or production load benchmark were tested. Those features
are not implemented in 0.1.

Windows CI repeats build, core simulations, desktop startup/recovery, packaging,
installation, and data-retention checks. Check the PR/release run for its actual
outcome rather than treating a workflow file as evidence of execution.
