# Install KevlarEDR on Windows

Version 0.1 provides a setup executable and a portable archive. See release notes
and validation evidence for the exact tested environment.

## Requirements

- Windows 11 x64; verified locally on Windows 11 Pro build 26200. Other Windows
  versions/architectures require separate validation.
- Permission to install/run a per-user application and write its local data.
- Space for the application, selected versions, and restored copies.
- Microsoft Defender installed/available for scanning. Recovery is independent
  of scanner availability. Company policy or permissions may restrict scans.

## Setup package

1. Obtain `KevlarEDR-Setup-0.1.0-win-x64.exe` and `SHA256SUMS.txt` from your
   local build's `artifacts` folder. Tagged releases will later provide these on the
   [release page](https://github.com/kingblessolivier/KevlarEDR/releases);
   hosted CI/merge are currently pending the GitHub account billing issue.
2. Compare SHA-256 using `Get-FileHash -Algorithm SHA256 <package-path>`.
3. Check release notes for unsigned prototype status. Do not disable antivirus,
   firewall, or other protections to install it.
4. Run the **Setup** executable (not the app inside a build folder). Review the
   destination and the desktop-shortcut/sign-in-startup options, then finish.
5. Open **KevlarEDR** from the Start Menu or its desktop shield shortcut. The
   installed executable is `%LOCALAPPDATA%\Programs\KevlarEDR\KevlarEDR.exe`.
6. Read the prototype limitations, add a disposable test folder, and capture its
   baseline before relying on version history for selected documents.

## Portable package

Extract the portable archive into a folder you control and launch `KevlarEDR.exe`.
Self-contained publishing includes the runtime. Data remains in the per-user local
data directory rather than next to the executable.

## Permissions and troubleshooting

If Defender rejects a scan, review its exact output and company policy. An
administrator may run the app with appropriate privileges for an explicit scan;
ordinary capture/restore does not need elevation. Do not bypass company controls.

If a file cannot be captured, check permissions, file locks, size limits, and
remaining vault capacity. Closing the window keeps the user app running in the notification area.
Right-click its shield beside the clock and choose **Quit (stop monitoring)** to
stop detection/observation. Settings can enable/disable startup when you sign in.
This is a user application, not a privileged service; signing out stops it.

## Uninstall and data

Use Windows Installed apps for a setup installation; delete only the extracted
application directory for a portable installation. Recovery data is retained.
Export important recovery copies before intentionally removing the data directory.
