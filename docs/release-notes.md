# KevlarEDR 0.1.0 — Windows prototype

Native Windows x64 app with drive inventory, selected-path Microsoft Defender
scan requests, local protected-folder versions, integrity-verified restore-as-copy,
persisted settings, and activity history.
The redesigned Windows workspace includes computer/disk/USB icons, real drive
capacity bars, component status cards, and searchable recovery/activity lists.

Observation runs only while the desktop app is open and occurs after changes.
This release does not block USB devices, intercept writes, contain ransomware,
run a Windows service, provide immutable backups, or support remote management.
Defender scans depend on its installation, permissions, and company policy.
The scan request disables automatic remediation and shows provider output.

The package is an unsigned development prototype. Checksums identify matching
bytes, not an authenticated publisher. Keep existing Windows protections enabled.
Use a separately protected backup for important documents.

Install the setup executable or extract the portable archive. See the installation
and user guides in the repository and portable package. The setup is per user;
uninstallation retains recovery data in Local AppData.

Release checks run byte-recovery simulations, failure-path validation, a native
desktop startup/page-render smoke test, and packaging on Windows.
