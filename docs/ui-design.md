# UI/UX design

## Direction

The user clarified that the interface should resemble File Explorer and expose
the actual filesystem and computer sections. The implemented WPF app starts with
This PC: a light navigation pane, real personal-folder and drive shortcuts, folder
tiles, used-capacity bars, a location field, search, and a command bar. Details and
Icons views show real folder entries; a side pane displays selected-file metadata.
Dark-mode and high-contrast theme switching remain future accessibility work.

The System section identifies the actual hostname, Windows build, and architecture.
Metric cards show observed folders, saved versions, and Windows-reported drives.
Disk and removable-drive cards use different icons and actual used-capacity bars.
The preview badge and observation lifetime remain visible; an icon or blue status
badge must not imply a threat-free or fully protected computer.

Navigation uses Windows' installed icon font. File/folder icons come from Windows
Shell by extension/synthetic attributes on a worker thread, with COM initialization
and explicit native-handle disposal. File contents are not read for icons and files
are not executed. No Windows icon font or extracted file icons are shipped.
Original shield/check artwork supplies application and installer icons. Labels and
automation names identify navigation, controls, searches, and lists. Custom control
templates include hover, selection, disabled, and keyboard-focus states.

The kernel does not draw the UI. Protection should eventually remain active in
a service when the UI closes; the prototype explicitly observes only while open.

## Navigation and real states

- This PC: real standard folders/drives; local folder navigation, metadata listing,
  filename filtering, hidden-items option, Details/Icons toggle, selection/details,
  and scan/preserve/recovery actions. No simulated filesystem.
- System: computer information, observation status, scanner, selected folders,
  stored versions, and next actions. No invented health percentage.
- Scans: choose path, request scan, show elapsed/outcome/output; distinguish engine
  unavailable, denied access, completion, and error.
- Devices: actual Windows drive inventory, reported type, space where accessible,
  refresh, and scan selection. Undeveloped blocking controls are not presented as
  working buttons.
- Recovery: searchable source/time/size records, restore-as-copy, and verified outcome.
- Activity: searchable local events and errors with timestamps.
- Settings: protected folders, baseline capture, version limits, and data location.
- Quarantine/advanced diagnostics: add only when corresponding operations exist.

## Key flows

This PC → drive/folder shortcut → folder contents → double-click a folder or enter
an absolute local path → inspect a selected file. Back/forward/up and Ctrl+L/Ctrl+F
support familiar navigation. Search filters the current folder's loaded names;
it does not index an entire disk. A failed navigation retains the previous location.
Enumeration is non-recursive and capped at 5,000 attempted entries with a visible
truncation message. Missing/unreadable locations show an error. Linked locations
are identified but not followed; network browsing requires a later milestone.

Protect folder → select directory → explain observer lifetime → capture baseline
→ show saved/skipped/failed counts → begin best-effort observation.

Recover file → choose preserved version → choose destination → verify bytes →
create a separate copy → show resulting path. A corrupt version must show a
failure, not a reassuring success banner.

Scan storage → choose actual drive/path → show provider availability → request
scan → retain full result → user reviews errors/unscanned content. Scan completion
does not automatically unlock future device policy.

## Accessibility and safety

Keyboard-accessible navigation/actions, descriptive labels, visible focus,
text alongside status colors, adequate contrast, and readable sizing. Frequent
operations stay easy to find; destructive or administrative actions need context
and clearly scoped targets. Never hide data-loss warnings behind hover.

The future company console reuses terms but adds computers/groups, incident queues,
policies, roles, deployment, and audit. The employee app remains focused on its own
computer; company-admin functions do not appear for ordinary employees.

The window supports resizing with a scrollable workspace (minimum 940 × 640).
Native smoke checks all seven sections, installed icon glyphs, search/selection,
shell file icons, file/folder navigation/history, failed navigation and compact
layout, exporting screenshots for visual review. This does not replace
manual keyboard, screen-reader, and DPI/usability testing.

References: [Windows navigation guidance](https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics),
[Microsoft Segoe Fluent Icons](https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-fluent-icons-font).
Shell references: [SHGetFileInfoW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow),
[DestroyIcon](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-destroyicon).
