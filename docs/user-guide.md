# KevlarEDR user guide

The sidebar provides Overview, Scans, Devices, Recovery, Activity, and Settings.
Computer, drive, USB, and folder icons identify the matching controls. Drive bars
show used storage capacity, not a security score. Recovery and Activity include
search fields; type part of a file path or event to filter the local records.

## Understand the first release

This app helps request Defender scans and keep versions of selected documents.
It is not complete ransomware prevention. Changes are observed after they happen;
earlier preserved versions are what make recovery possible. Close the desktop app
and its observation stops. Keep an additional separately protected backup.

## First use

1. Install/open the app using the installation guide.
2. Review Overview for scanner availability and observation status.
3. In Settings, add a disposable folder containing a few test documents.
4. Capture a baseline and review saved/skipped/failed results.
5. Change or delete a test file and restore a previously captured version to a
   separate folder. Confirm the contents before protecting important folders.

## Protect a folder

Select a folder in Settings and capture its baseline. The app records selected
folders for the next launch. Avoid system directories, the app's vault, links,
and folders containing sensitive files you do not intend to copy into the vault.
Files outside supported size/permission/format conditions can be skipped or fail;
review activity rather than assuming every document was saved.

Capture another baseline manually after substantial work or an observation warning.
Removing a folder stops observing it; existing versions remain recoverable.

## Scan a flash drive or folder

Connect the drive, refresh Devices, select the appropriate drive, and request
a scan; alternatively choose a folder in Scans. Review provider output and status.
The prototype requests a scan without automatic remediation. Errors, permission
failures, and unscanned files need attention. Completion is not proof of safety.
Device blocking/read-only enforcement is planned and is not available in 0.1.

## Recover a file

1. Open Recovery and select the source filename and capture time you need.
2. Choose a separate destination folder.
3. KevlarEDR verifies the stored bytes and writes a new copy.
4. If a filename exists, the app uses a different name rather than replacing it.
5. Open the recovered file and inspect it before replacing a current document.

If integrity verification fails, the app refuses that restore. If no version was
captured, KevlarEDR cannot reconstruct the missing bytes. Separate undelete tools
may help in some circumstances; overwritten space can make recovery impossible.

## Activity and storage

Activity records captures, skips/errors, scans, and restores. Settings shows local
data location and configured limits. A full vault declines new captures while
retaining earlier versions. Never erase the vault to clear a warning before
exporting recovery copies you need.

## Company management later

Remote control, directory enrollment, central policy, and support sessions are
roadmap features. They will require a company server, authenticated agent, and
role-based authorization. The local prototype does not silently enroll a computer.
