# Architecture decision records

## ADR-001: Begin with one Windows computer

Accepted by user, 2026-10-09. Deliver the local experience first while retaining
company management in the roadmap. Consequence: no remote server is necessary
for local capture/restore, and enterprise features cannot be claimed yet.

## ADR-002: Windows executable/installer

Accepted by user, 2026-10-09. Deliver `.exe` or `.msi`; Android APK is not the
endpoint package. Windows x64 is the initial build target. Additional architectures
require their own packages and validation.

## ADR-003: C#/.NET desktop prototype

Implementation decision. Use the available .NET 10/WPF toolchain for functional
local UI and recovery. Keep core logic separate. Rust service/C minifilter remain
future components, justified by privilege and kernel needs rather than prestige.

## ADR-004: Observe changes; do not claim pre-write guarantees

Filesystem notifications are suitable for prototyping version history. They do
not pause writes or preserve every original. Initial baseline/manual capture is
explicit; kernel-backed preservation is a separate milestone.

## ADR-005: Restore a copy, never silently replace

Preserve current files and verify recovered bytes. The first restore destination
is chosen by the user and filename collisions produce a new name. Future incident
rollback requires a reviewed operation plan and containment first.

## ADR-006: Reuse the installed scanner initially

Use supported Defender custom scans and expose their outcomes. Do not manufacture
malware verdicts from hashes or entropy. Engine updates remain Defender's task.
