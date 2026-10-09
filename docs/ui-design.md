# UI/UX design

## Direction

The user requested an OS-style interface with computer icons after rejecting the
plain initial screen. The implemented WPF app uses a navy navigation rail, a light
Windows workspace, rounded surfaces, native Segoe Fluent Icons, system typography,
explicit action labels, and real computer/storage information. WinUI is not required
to preserve this information architecture. Dark-mode and high-contrast theme
switching remain future accessibility work.

The computer panel identifies the actual hostname, Windows build, and architecture.
Metric cards show observed folders, saved versions, and Windows-reported drives.
Disk and removable-drive cards use different icons and actual used-capacity bars.
The preview badge and observation lifetime remain visible; an icon or blue status
badge must not imply a threat-free or fully protected computer.

Icons use Windows' installed font with a Segoe MDL2 fallback; no icon font is
redistributed. Original shield/check artwork supplies application and installer
icons. Each icon is paired with a text label. Automation names identify navigation,
buttons, searches, lists, scanner output, and storage bars. Custom control templates
include hover, selection, disabled, and keyboard-focus states.

The kernel does not draw the UI. Protection should eventually remain active in
a service when the UI closes; the prototype explicitly observes only while open.

## Navigation and real states

- Overview: monitoring status, available scanner, protected folders, stored versions,
  and next useful actions. No invented health percentage.
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
Native smoke checks all six pages, 14 installed icon glyphs, search/selection,
and compact layout, exporting screenshots for visual review. This does not replace
manual keyboard, screen-reader, and DPI/usability testing.

References: [Windows navigation guidance](https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics),
[Microsoft Segoe Fluent Icons](https://learn.microsoft.com/en-us/windows/apps/design/iconography/segoe-fluent-icons-font).
