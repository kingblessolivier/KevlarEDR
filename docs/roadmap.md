# Detailed development roadmap

This is the design roadmap. Live task status is managed through the project's
TODO connector. Versions below are milestone labels, not promised delivery dates.
Each milestone requires demonstrated acceptance evidence before the next release.

## 0.1: Usable local desktop prototype

Deliverables: WPF app; independent core; persisted folder settings and activity;
Windows drive discovery; selected-path Defender integration; explicit baseline;
best-effort change observation; content-addressed vault; verified restore-as-copy;
self-contained x64 package; installation/user guide; build/validation CI.

Sequence: document requirements → implement safe storage → validate byte recovery
and failure paths → add observer and inventory → add scan adapter → integrate UI
→ run simulations → package → clean-user installation smoke test → feature PR.

Exit gate: capture, overwrite, delete, restart, restore, corruption, collision,
storage-limit, unsafe-path, and multi-instance cases tested; no success shown for
failed scans/restores; package starts on Windows; no driver/security setting change.

## 0.2: Background service and secure local IPC

Dependencies: stable version format, measured observer overhead, installer design.
Deliverables: Rust Windows service, authenticated local IPC, service recovery,
machine/user data separation, independent UI, secured configuration, and migrations.

Design requests by operation type and caller scope. Use bounded request sizes,
authorization, cancellation, and deadlines. Separate diagnostics from privileged
policy changes. Model upgrade/service restart while an operation is pending.

Exit gate: UI exit does not stop observation; unauthorized callers rejected;
service crashes/restarts preserve valid metadata; service account cannot access
unnecessary network credentials; uninstall preserves recovery data by default.

## 0.3: Removable-storage access policy

Dependencies: privileged operation authorization and VM policy lab.
Deliverables: distinct read/write/execute policies, approved devices, temporary
exceptions, scanner-specific access, policy targeting, and reversible enforcement.

Assess supported Windows controls before building a storage filter. Device IDs
are identification signals, not unforgeable trust. Handle fixed-class USB disks,
composite devices, changing drive letters, boot volumes, and unavailable volumes.

Exit gate: read/write/execute test matrix, reconnect/reboot scenarios, expired
exceptions, denied ordinary-user changes, scanner access without ordinary unlock,
and recovery from misconfiguration without losing keyboard/boot access.

## 0.4: Kernel-backed file preservation

Dependencies: signed driver distribution plan, WDK/SDK build, disposable VM lab,
secure filter-port protocol, service health/failure policy, and capture performance.
Deliverables: minimal C minifilter, targeted protected-file policy, file identity
tracking, supported pre-write/pre-delete preservation, and transaction/recovery logs.

Cover create/truncate/overwrite, write, rename-replace, delete-on-close, disposition,
memory-mapped and cached I/O, alternate streams, hard links, reparse points, and
unsupported filesystem behavior. Define coverage before claiming comprehensive
protection. Avoid copying files or indefinitely waiting inside callbacks.

Wire protocol: fixed-width types, explicit schema/version/length, correlation IDs,
validated reply size, operation-specific deadline, protected caller identity, and
disconnect/timeout handling. `STATUS_TIMEOUT` must be handled explicitly; merely
checking `NT_SUCCESS` is insufficient for `FltSendMessage`.

Exit gate: VM Driver Verifier/stress/reboot/unload/disconnect/disk-full tests,
documented coverage gaps, preserved coherent originals in supported scenarios,
no deadlock during service I/O, and measured normal-application latency.

## 0.5: Behavioral detection and incident response

Dependencies: reliable telemetry and preserved versions.
Deliverables: process identity/correlation, multiple behavioral signals, explainable
alerts, audit mode, policy thresholds, narrowly scoped exceptions, containment,
network isolation with a management path, and incident recovery plans.

Evaluate document editors, compilers, archives, backup tools, sync applications,
bulk renames, and benign encryption alongside synthetic destructive workloads.
Protect critical processes and validate identity against PID reuse before response.
Evidence must support automatic action; high entropy alone is insufficient.

Exit gate: report detection latency, files modified/lost before containment,
false-positive rate with workload definitions, restore success, resource overhead,
and response reversibility. Do not market complete malware elimination.

## 0.6: Recovery hardening and peripheral controls

Dependencies: operational response and safe policy recovery.
Deliverables: backup encryption/key lifecycle, external/offline copy workflow,
integrity scrubbing, retention safeguards, recovery health, and separate policies
for supported HID/network interfaces. Add confidential-copy policy where useful.

BadUSB defense requires interface-specific enforcement and enrollment policy;
typing-speed heuristics alone can misidentify accessibility tools or automation.
Do not claim firmware inspection or universal hardware protection.

Exit gate: recover after local-vault loss using external copy; test key loss/rotation;
verify trusted input remains usable; test composite devices and policy rescue.

## 0.7: One-company remote management

Dependencies: service identity, secured updater, durable audit, and offline policy.
Deliverables: on-premises-capable API/web console, unique agent enrollment, TLS,
device inventory/last contact, role/action/device scopes, expiring commands,
central policy, action history, and optional limited-permission AD import.

Command lifecycle: queued → delivered → accepted/running → succeeded/failed;
also expired/cancelled/rejected. Specify retries and idempotency. Isolation retains
only the defined management channel. Directory import does not grant endpoint
control; agent enrollment authenticates computers separately.

Exit gate: authorization on every operation; expired/offline/replayed commands
rejected; stale status visible; limited branch role cannot access other branches;
bulk-action preview and staged policy deployment tested; no domain-admin secret
stored in endpoints. Interactive remote support has separate access and session policy.

## 0.8: Multiple companies and provider operations

Dependencies: proven single-company console and security review.
Deliverables: tenant isolation, delegated/time-bounded support, separate company
keys and data scopes, company export/retention, release rings, and service health.

Exit gate: cross-tenant API/query/command tests; revocation and key-rotation drills;
restore/export boundaries; provider operator access is explicit and auditable.

## Research and readiness work throughout

Maintain traceability from FR/NFR IDs to code/validation. Benchmark on specified
low-resource hardware rather than guessing performance. Maintain dependency and
signing inventories. Add localization after core terminology and flows stabilize.
Choose a project license before soliciting outside contributions/commercial release.

Reference: [FltSendMessage semantics](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/fltkernel/nf-fltkernel-fltsendmessage).
