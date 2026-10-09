# System analysis and requirements

Status: design baseline, 2026-10-09. Owner: KevlarEDR maintainer.

## Problem and intended users

Windows users need to inspect removable storage, understand suspicious events,
and recover selected documents after accidental deletion or malicious damage.
The initial user operates one computer. Future users include company security
teams, help desks, branch administrators, auditors, and employees.

Existing endpoint products already provide scanning, device policy, offline
prevention, and rollback. The research contribution is the measured combination
of accessible local controls, recovery transparency, offline operation, and
resource efficiency for the intended environment; novelty is not assumed.

## System boundary

Initial release: Windows desktop UI, local recovery vault, file-change observer,
Windows drive inventory, Defender scan adapter, settings, and activity records.
It does not replace the operating system, implement its own antivirus, intercept
all writes, or run after the desktop process exits.

Future boundary: privileged service, minimal minifilter, separate device policy,
behavioral correlation, response actions, management server, and authenticated
endpoint communications. Android packaging is out of scope; the user selected
a Windows executable/installer.

## Functional requirements

| ID | Requirement | Milestone | Acceptance evidence |
|---|---|---|---|
| FR-01 | List accessible drives and distinguish reported drive types | 0.1 | Compare inventory to Windows; inaccessible volumes do not crash UI |
| FR-02 | Request a selected-path Defender scan and retain its result | 0.1 | Existing file/folder accepted; missing scanner and failures explained |
| FR-03 | Add/remove protected folders and persist configuration | 0.1 | Configuration survives app restart; overlapping/unsafe paths rejected |
| FR-04 | Capture explicit baseline versions and observed later changes | 0.1 | Saved version bytes match source; failures are logged |
| FR-05 | Browse versions by source, time, size, and integrity | 0.1 | Records survive restart; missing/corrupt blobs shown as unavailable |
| FR-06 | Restore a verified copy without overwriting existing files | 0.1 | Byte-for-byte equality and destination collision scenarios |
| FR-07 | Show real operation history and component health | 0.1 | No fabricated telemetry; monitoring lifetime clearly shown |
| FR-08 | Enforce bounded file size, vault size, and metadata retention | 0.1 | Budget overflow preserves earlier versions and reports failure |
| FR-09 | Run independently of the UI in a Windows service | 0.2 | Closing UI does not stop configured protection |
| FR-10 | Control removable-storage read/write/execute separately | 0.3 | VM policy matrix and reversible exceptions |
| FR-11 | Preserve supported originals before destructive operations | 0.4 | Pre-write/pre-delete VM scenarios and failure-policy validation |
| FR-12 | Detect suspicious behavior using multiple signals | 0.5 | Labeled workload results with false positives and files lost |
| FR-13 | Contain incidents and recover affected versions | 0.5 | Process identity validated; critical processes excluded; audit trail |
| FR-14 | Support distinct peripheral-interface policies | 0.6 | HID/network/storage tests; trusted input remains usable |
| FR-15 | Manage enrolled company endpoints by scoped roles | 0.7 | Authorization and disconnected-command scenarios |
| FR-16 | Support directory import and group-based policy targeting | 0.7 | Limited-permission AD integration and membership changes |
| FR-17 | Support tenant separation and delegated provider support | 0.8 | Cross-tenant requests denied in automated tests |

## Nonfunctional requirements

| ID | Requirement | Initial validation / later target |
|---|---|---|
| NFR-01 | Avoid silent data loss | Restore never overwrites; manifests committed after blobs |
| NFR-02 | Be usable without cloud access | Local capture/restore works with network disabled |
| NFR-03 | Keep privilege limited | Desktop runs per user; unavailable elevated scans are explicit |
| NFR-04 | Bound resource use | 32 MiB default maximum source file, 512 MiB vault, bounded event queue |
| NFR-05 | Make status honest | Unknown/disconnected/error states differ from healthy |
| NFR-06 | Provide accessible UI | Keyboard navigation, descriptive labels, scalable text, contrast checks |
| NFR-07 | Produce reproducible releases | Pinned SDK policy, lock/build inputs, checksummed artifacts |
| NFR-08 | Maintain privacy | No automatic document upload; local paths excluded from public CI logs |
| NFR-09 | Survive interruptions | Atomic metadata writes; orphan blobs detected rather than trusted |

Targets for later service/driver releases must be measured on specified hardware.
No unmeasured CPU, memory, response-time, or recovery guarantee is accepted.

## Use cases

UC-01: user connects storage → inventory updates → chooses scan → reviews full
scanner outcome. Device presence is not an automatic declaration of safety.

UC-02: user selects a folder → baseline capture records successes and failures →
observer captures later readable changes while the app process runs, including in the notification area.

UC-03: user loses a document → selects an earlier preserved version → chooses
another destination → integrity verified → new file created → event recorded.

UC-04 (future): analyst investigates an incident → authorizes isolation → endpoint
executes an authenticated, unexpired command → management connection retained →
verified versions restored under an explicit recovery plan.

## Constraints and risks

Filesystem notifications occur after changes and can be lost. They cannot ensure
pre-write preservation. Concurrent writes can invalidate a capture. Local backups
can be damaged by the same user, an administrator, hardware failure, or malware.
Defender integration depends on installation, policy, permissions, and engine state.
Not every USB disk is classified as removable by Windows. Each limitation belongs
in the UI and user guide, not only in a developer comment.

## Sources

- [Microsoft device control](https://learn.microsoft.com/en-us/defender-endpoint/device-control-walkthroughs)
- [Defender scan commands](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus)
- [Windows File Recovery limitations](https://support.microsoft.com/en-us/windows/experience/backup-recovery/windows-file-recovery)
- [SentinelOne endpoint platform](https://www.sentinelone.com/platform/endpoint-protection-platform/)
