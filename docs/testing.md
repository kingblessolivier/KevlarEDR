# Testing and simulation strategy

## Initial validation

Use a disposable temporary tree with synthetic text/binary files and a separate
vault. Validation must fail with a nonzero exit code on any failed assertion.
Do not modify real protected folders or run real malware.

Required scenarios:

1. Capture original, modify source, capture again, and restore the first version.
2. Delete source after capture and restore the preserved original.
3. Reopen the vault in another instance and restore persisted records.
4. Corrupt a blob and confirm restore refusal without a success file.
5. Restore into an existing filename and confirm the existing bytes remain intact.
6. Exceed source/blob limits; confirm prior versions remain recoverable.
7. Reject vault-contained and reparse-point source/destination paths.
8. Exercise concurrent captures and independent-process vault locking.
9. Start monitoring, modify fixtures, and confirm a later version or explicit error.
10. Confirm malformed/unsupported metadata fails visibly.

Scanner tests cover target validation, provider absence, denied execution, bounded
output, exit-code ambiguity, and UI error handling. Real-provider smoke tests are
recorded separately; do not use a fake scanner to claim malware detection.

## Build and UI validation

Windows CI restores/builds, runs core validation, publishes x64 artifacts, and
generates hashes. Desktop smoke mode opens the real WPF window and closes it
after checking initialization; it cannot substitute for manual interaction tests.
Validate drive list, folder chooser, baseline summary, version selection, restore
destination, scan outcome, status/observer lifetime, and keyboard accessibility.

## Packaging and installation

Verify packaged executable starts; inspect installer contents; install into a
disposable per-user directory; verify app shortcut/uninstall behavior; uninstall
without deleting recovery data. Record unsigned status and actual tested OS/build.
Check published artifact hashes against the packaged bytes.

## Later validation

Driver: VM snapshots, kernel debugger, Driver Verifier, reboot/unload, resource
pressure, timeout/disconnection, filesystem coverage, and latency benchmarks.
Behavior engine: labeled benign/destructive workloads with false positives,
latency, affected-file counts, and verified recovery.
Remote management: unauthorized roles, scopes, enrollment/revocation, retries,
expiration/replay, isolation management path, offline/stale states, and tenants.

## Evidence

Keep reproducible commands and sanitized results in `docs/validation.md`. Record
failures and limitations alongside successes. Never write “all tests passed” when
only compilation was performed, or claim production protection from simulation.
