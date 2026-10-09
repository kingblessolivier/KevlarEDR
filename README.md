# KevlarEDR

Windows removable-media scanning and recoverable file versions, with a roadmap
toward kernel-backed endpoint protection and company management.

## Release scope

The first release is a **single-computer desktop prototype**, not a replacement
for Microsoft Defender or a production EDR. It requests scans from Defender,
discovers Windows drives, preserves versions of selected files, and restores
verified copies. Filesystem monitoring runs while the desktop app is open.

USB blocking, pre-write protection, an always-running privileged service,
ransomware containment, immutable backups, and remote administration belong to
later milestones. A local version vault is not an air-gapped backup.

## Install and use

See [Installation](docs/installation.md) and [User guide](docs/user-guide.md).
Tagged packages will be published on the [release page](https://github.com/kingblessolivier/KevlarEDR/releases).
The locally tested prototype is in [PR #1](https://github.com/kingblessolivier/KevlarEDR/pull/1);
hosted CI and merge are pending a GitHub account billing lock. See the
[validation evidence](docs/validation.md) for completed local checks.
Initial target: Windows 11 x64 (locally tested on Pro build 26200).
Windows x64 packages are produced under `artifacts/` by the packaging script.
The self-contained application does not require a separate .NET runtime.
Defender must be installed and available for its scan integration.

The Windows interface includes native computer/drive/USB icons, actual storage
capacity bars, component status, and searchable recovery and activity workspaces.

## Develop

Install the .NET 10 SDK on Windows, then run:

```powershell
dotnet build KevlarEDR.slnx -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release
dotnet run --project src/KevlarEDR.Desktop
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1
```

Do not run destructive simulations on real documents. Validation uses temporary
fixtures and restores to separate destinations.

## Documentation

- [System analysis and requirements](docs/system-analysis.md)
- [Architecture and component design](docs/architecture.md)
- [Data and recovery design](docs/data-design.md)
- [Security and threat model](docs/security-design.md)
- [UI/UX design](docs/ui-design.md)
- [Detailed roadmap](docs/roadmap.md)
- [Testing and simulation strategy](docs/testing.md)
- [Actual validation evidence](docs/validation.md)
- [Feature → PR → merge workflow](docs/git-workflow.md)
- [Release and deployment design](docs/release-design.md)
- [Installation](docs/installation.md) and [User guide](docs/user-guide.md)
- [Contributing](CONTRIBUTING.md), [Security reporting](SECURITY.md),
  [Changelog](CHANGELOG.md), and [architecture decisions](docs/decisions.md)

## Repository

https://github.com/kingblessolivier/KevlarEDR

No license has been selected yet. Public visibility does not grant a software
license. Contributors must not add third-party code without compatible terms.
