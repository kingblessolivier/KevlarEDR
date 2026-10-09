# UI/UX design

## Direction

Native Windows desktop interface with restrained colors, a left navigation pane,
clear action labels, scalable system typography, and light/dark/high-contrast
support as implementation matures. The first prototype uses WPF; WinUI is not
required to preserve this information architecture.

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
- Recovery: actual source/time/size records, restore-as-copy, and verified outcome.
- Activity: local events and errors with timestamps.
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

Reference: [Windows navigation guidance](https://learn.microsoft.com/en-us/windows/apps/design/basics/navigation-basics).
