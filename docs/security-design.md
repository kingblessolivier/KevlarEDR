# Security design and threat model

## Assets and adversaries

Assets: original documents, valid recovery versions, policies, scanner results,
device identities, future enrollment keys, and administrative command records.
Adversaries: untrusted files/devices, malicious ordinary-user processes, compromised
administrators, malformed metadata/IPC inputs, and future compromised accounts.

The prototype does not promise protection against same-user vault modification,
local administrator/kernel compromise, physical disk failure, or firmware attacks.

## Threats and mitigations

| Threat | Initial mitigation | Later requirement |
|---|---|---|
| Restore path traversal | Validated filenames/destinations, no reparse traversal, exclusive create | Service authorization and race-resistant handle-relative paths |
| Corrupted recovery bytes | Size/hash verification before and during restore | Authenticated metadata and separately protected backup |
| Partial metadata writes | Same-directory temporary file and atomic replacement | Transactional journal and migration recovery |
| Event/large-file exhaustion | Bounded queue/file/blob/record limits | Per-process rate limits and prioritized driver queues |
| False clean scan | Preserve raw outcome, unavailable/error states | Normalized provider verdict with evidence |
| Malicious filename in shell | Structured ProcessStartInfo arguments; no shell interpolation | Fuzzed privileged request parser |
| Same-user vault deletion | Explain limitation and recommend separate backup | Service ACLs, controlled maintenance, external copy |
| Compromised management account | No remote control in prototype | MFA, scoped roles, short-lived authorization, audit |
| Cross-company access | No multi-tenancy in prototype | Tenant checks in every query and command path |
| Replay/stale remote command | No remote commands in prototype | Signed/authenticated identity, nonce, expiration, idempotency |
| Update supply-chain compromise | Checksums and CI evidence | Signed binaries, pinned dependencies, staged updates |

## Privilege design

Run desktop/capture/restore as the current user. Scan integration may fail without
the permission Defender requires; explain it rather than silently elevating or
weakening Defender. Do not disable antivirus, firewall, Secure Boot, or integrity
protections to make the desktop app work.

Future service code must use the minimum account rights and separate view/scan/
restore/device-policy/containment permissions. An administrator's UI session is
not a reusable secret for all endpoints. Live remote support is separately scoped.

## Safe testing

Use disposable fixtures for overwrite/delete/corruption simulations. Never generate
or execute real ransomware. EICAR testing, if desired, belongs in an explicit
isolated lab and is not necessary for verifying recovery correctness. Driver
experiments belong in disposable VMs with snapshots and host-based debugging.

## Release security gates

Before privileged enforcement: secure IPC tests, reentrancy/deadline handling,
driver signing, verifier/stress/reboot tests, critical-process exclusions,
tamper limits, and recovery evidence. Before remote control: authorization tests,
device enrollment/rotation/revocation, audit completeness, tenant separation,
and expired/offline command tests. See roadmap for milestone gates.

## Privacy

Documents never upload automatically. Path/device/scan output can contain sensitive
information. Export is user initiated; public issues and CI must use synthetic data.
Future fleet logging needs field-level minimization, retention controls, access
scopes, and company-approved collection policies.
