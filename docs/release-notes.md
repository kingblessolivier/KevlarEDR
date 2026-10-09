# KevlarEDR 0.1.0 — Windows prototype

Native Windows x64 app with drive inventory, selected-path Microsoft Defender
scan requests, local protected-folder versions, integrity-verified restore-as-copy,
persisted settings, and activity history.
The Windows filesystem workspace opens to This PC with real folders/drives,
native file-type icons, local browsing, Details/Icons views, location/history,
filename filtering and file details. System shows local computer/component facts.
Recovery/activity lists remain searchable. Linked/network browsing is not supported.
Local volume connections/removals refresh automatically and are recorded in Activity.
New drive banners offer Open/Scan. Closing hides to the notification area; Start Menu
and desktop shortcuts reopen one instance. Setup/Settings offer per-user sign-in startup.

Observation occurs after changes while the process runs, including in the notification area; Quit/sign-out stops it.
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
