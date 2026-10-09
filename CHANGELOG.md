# Changelog

Changes are recorded by release. Dates use YYYY-MM-DD. Planned work belongs in
the roadmap, not under released functionality.

## 0.1.0 - Local prototype, 2026-10-09 (not yet published)

### Added

- System analysis, requirements, architecture, data design, threat model, UI
  design, detailed roadmap, test strategy, and contribution/release workflow.
- Persistent project decisions: begin with one Windows computer, then expand
  to managed company endpoints.

- Native Windows desktop app, drive discovery, Defender scan requests,
  local protected-file versions, verified restore-as-copy, and activity history.
- Self-contained Windows x64 executable and installation package.
- Automated recovery simulations and Windows CI.
- Post-change observation of selected folders while the desktop app is open.
- 31 core validation scenarios plus native desktop and installer smoke tests.
- Windows control-center UI with native computer, drive, USB, folder, scan,
  recovery, activity, and settings icons; real storage bars and component panels.
- Searchable recovery/activity lists, keyboard-focus states, compact-window
  rendering, and original executable/window/installer icon artwork.

### Delivery status

- Feature PR #1 is open and `main` protection requires Windows validation.
- GitHub cannot start hosted CI because of an account billing lock. Local checks
  are separate evidence; hosted validation, merge, and tagged release are pending.

### Limitations

- No privileged background service, pre-write protection, USB blocking, malware
  containment, immutable backups, or remote management in this prototype.
- Unsigned binaries; Windows 11 x64 is the first tested target.
