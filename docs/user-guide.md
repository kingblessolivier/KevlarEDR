# KevlarEDR user guide

The app opens to This PC, like a Windows filesystem workspace. The sidebar also
provides Scans, Devices, Recovery, Activity, Settings, and System, plus standard
folder and drive shortcuts. Drive bars show storage capacity, not a security score.

## Browse the computer

1. Select a personal folder or drive in This PC or the sidebar.
2. Double-click a folder to enter it. Files show name, modified time, type and size.
3. Select a file to view its information in the Details pane.
4. Use Back, Forward or Up; Ctrl+L focuses the location field. Enter a full local
   folder path and press Enter. Ctrl+F focuses the filename filter.
5. Choose Details or Icons. Hidden items can be included with the checkbox.
6. Select a file/folder and choose Scan selected, Preserve folder, or Saved versions;
   the same actions are available in the list's right-click menu.

Search filters loaded names in the current folder. A folder listing is capped at
5,000 entries and displays when truncated. Browsing reads metadata; file content
is not opened or executed. Linked/network locations are not traversed in this preview.
The System section displays local computer/Windows/component information.

## Understand the first release

This app helps request Defender scans and keep versions of selected documents.
It is not complete ransomware prevention. Changes are observed after they happen;
earlier preserved versions are what make recovery possible. Closing the window leaves observation and device detection running in the
notification area. Right-click the shield beside the Windows clock and choose
**Quit (stop monitoring)** to stop them. Keep an additional separately protected backup.

## First use

1. Install/open the app using the installation guide.
2. Review System for scanner availability and observation status.
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

Insert a flash drive while KevlarEDR is running. This PC, the drive sidebar and
Devices update automatically. A connection banner offers **Open drive** and
**Scan drive**; the notification area also shows an alert when Windows permits
notifications. Detection also notices new external volumes reported as Fixed.
Choose **Scan drive** and review its result; scans are explicitly requested, not
automatically started. Removal updates the lists; browsing a removed drive
returns to This PC and clears stale actions. Alternatively choose a folder in Scans. Review provider output and status.
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

Activity records drive connections/readiness/removals, captures, skips/errors, scans, and restores. Settings shows local
data location and configured limits. A full vault declines new captures while
retaining earlier versions. Never erase the vault to clear a warning before
exporting recovery copies you need.

## Company management later

Remote control, directory enrollment, central policy, and support sessions are
roadmap features. They will require a company server, authenticated agent, and
role-based authorization. The local prototype does not silently enroll a computer.
