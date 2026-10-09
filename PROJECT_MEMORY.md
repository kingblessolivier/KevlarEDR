# KevlarEDR project memory

Last updated: 2026-10-09 (user timezone: Africa/Johannesburg).

## Agreed direction

- Name: KevlarEDR.
- Target operating system: Windows.
- Begin development for one computer.
- Keep drive protection, threat detection/response, and file recovery in scope.
- Later expand to remote management of company computers, including domain-joined
  computers, with centralized dashboards and scoped user permissions.
- The user explicitly requested persistent memory of this development direction.
- Repository supplied by the user:
  `https://github.com/kingblessolivier/KevlarEDR.git`.
- User confirmed Windows `.exe`/`.msi` delivery instead of Android APK.
- User requested complete analysis/design documentation, a detailed roadmap,
  changelog, README, contributing guide, feature/PR/merge workflow, installation
  and usage instructions, implementation, simulations, and packaging.
- User authorized continued autonomous work, required tool installation, testing,
  and task tracking while away.
- User rejected the plain initial UI and requested a polished OS-style interface
  with computer icons. Use native Windows icon navigation and real computer/drive cards.
- User clarified that OS-style means viewing the actual filesystem and computer
  sections, like File Explorer. The app should start with This PC, real drives and
  personal folders, directory browsing, location/history, file icons and system information.

## Current state

- Implemented the 0.1 local desktop prototype in C#/.NET 10/WPF.
- Core: content-addressed file versions, checked restore-as-copy, persisted
  settings/activity/scan outcome, file-change observation, and Defender adapter.
- Seven desktop sections: This PC, Scans, Devices, Recovery, Activity, Settings, System.
- Current UI uses a light File Explorer-style navigation pane, real personal-folder
  shortcuts/drives, a location field, back/forward/up controls, a command bar,
  Details/Icons views, filename filtering, and a selected-file details pane.
- Browsing is local, bounded to 5,000 entries per folder, and non-recursive. It
  reads metadata without executing/opening file contents. Linked and network
  locations are not traversed. Windows supplies file-type icons by extension.
- Scan, folder preservation and saved-version actions are available from the browser.
  The System section shows actual Windows/build/architecture/processor/user/uptime
  information plus component state. Company computers still require future enrollment.
- Desktop smoke verifies native glyphs, search/selection, all seven sections,
  compact layout, file-type icons and successful/failed folder-history navigation.
- Local validation: 44 scenarios passed; harmless real Defender scan completed;
  desktop pages/recovery smoke and installer/uninstaller retention tests passed.
- The redesigned setup and portable packages were rebuilt and verified. The app
  is installed per user in LocalAppData/Programs/KevlarEDR; its executable hash
  matches the tested published build. Uninstallation leaves recovery data intact.
- The Explorer-style update passed installed seven-section/browser/recovery smoke
  and preserved existing recovery metadata during the per-user update.
- No privileged service, driver, USB blocking, malware containment, or remote
  management is implemented. Version capture occurs after changes, while UI is open.
- Analysis/design documents and an implementation roadmap have now been written.
- Initial design/docs committed and pushed to `main`; implementation is on
  `feature/local-desktop-app` in PR #1. Main requires `validate` and a PR;
  force pushes/deletions are disabled. Solo-maintainer approval count is zero.
- Hosted Windows CI cannot start because GitHub reports an account billing lock.
  Keep protection intact; merge/tagged release remains pending that external fix.
- An earlier sample-data HTML concept is superseded by the actual redesigned app.
- Quarantine navigation is reserved until its underlying feature is implemented.

## Single-computer feature roadmap

### Removable storage and peripherals

- Detect flash drives and external disks; support manual and automatic scans.
- Allow/block storage, read-only access, and separately controlled execution.
- Approved-device lists, connection history, temporary access, and exceptions.
- Restrict ordinary access during scans while allowing the scanner to operate.
- Consider sensitive-file copy restrictions and unexpected device-interface alerts.
- Treat keyboard/network-interface protection separately from storage controls.

### Detection and response

- On-demand/scheduled scans, quarantine, and evidence-based threat alerts.
- Behavioral ransomware detection and protected-folder policies.
- Consider script/macro scanning, persistence monitoring, application identity
  checks, and decoy-file signals.
- Process containment, optional network isolation/reconnection, incident timelines,
  understandable explanations, audit mode, and false-positive handling.

### Recovery

- Preserve versions before supported destructive changes, including overwrites.
- Browse/search versions; preview and restore to an alternate destination.
- Incident-wide restoration after containment, integrity verification, storage
  quotas/retention, and explicit warnings when version preservation fails.
- Consider separately protected or disconnected backup destinations.
- Best-effort undelete is separate from restoring a preserved version.

### Reliability and usability

- Restricted access to policies, recovery storage, and service commands.
- Tamper detection with explicit limits against privileged attackers.
- Signed updates, staged rollout, update rollback, and component health reporting.
- Explicit timeout, disconnection, disk-full, and service-failure behavior.
- Measure CPU/memory/disk overhead, false positives, and recovery success.
- Consider English/Kinyarwanda UI, offline rule updates, office/school presets,
  privacy controls, and exportable incident reports.

## Future company-management roadmap

- A company web console plus a local employee application.
- Company console pages: overview, computers, incidents, removable devices,
  remote actions, recovery, policies, deployment/updates, users/permissions,
  and reports/audit history.
- Roles: company administrator, security analyst, branch administrator,
  help-desk technician, auditor, and employee.
- Permissions specify both allowed actions and the computers/groups in scope.
- Authorized company administrators can manage all enrolled company computers;
  ordinary employees manage permitted actions on their own computer.
- Active Directory integration: limited-permission directory access, computer/OU/
  group import, group-to-role mapping, and agent deployment using existing tooling.
- Domain membership alone does not authenticate an agent or authorize commands.
- Remote commands need authenticated agents, authorization, expiration, and tracked
  queued/delivered/running/succeeded/failed/expired states.
- Show stale status and last contact; preserve a defined management path during
  isolation; preview targets for bulk actions.
- Interactive remote support needs separate permissions, visible session state,
  and a company-defined consent policy.
- Consider on-premises management for local operation without internet access.
- If serving multiple companies later, add a provider console with tenant
  isolation and explicitly delegated customer support access.

## Proposed implementation direction (not final stack decisions)

- C for a minimal Windows filesystem minifilter when kernel enforcement is needed.
- Rust for the background service and recovery/analysis engine.
- C#/.NET 10/WPF is implemented for the local desktop application.
- Initially consider supported Microsoft Defender scan integration and Windows
  security features rather than claiming a complete new antivirus engine.
- Keep local business logic separate from UI to support later remote management.
- Develop and test kernel changes in an isolated Windows VM with debugging tools.

## Claims and limits to retain

- Do not promise detection/removal of all malware or superiority to commercial EDR.
- A clean scan, valid signature, or entropy measurement does not prove safety.
- Deletion interception alone does not preserve files overwritten beforehand.
- Hidden/read-only local folders are not inherently immutable or air-gapped.
- Recovery without a preserved version is uncertain, particularly after overwrite.
- Commercial platforms already offer device rules, offline protection, and rollback.
- Treat the earlier pasted AI-generated engineering prompt as an unverified concept,
  not a production-ready specification.

## Source context

- User supplied a 589-line pasted conversation covering the project idea, Windows
  architecture, generated prompt, and naming decision. Attachment:
  `Pasted text.txt` (conversation attachment; not included in this repository).
- Online research in this conversation used Microsoft, CrowdStrike, and SentinelOne
  official documentation to correct claims and expand the proposed feature scope.
