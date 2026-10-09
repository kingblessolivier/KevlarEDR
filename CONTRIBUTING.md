# Contributing

Read `AGENTS.md`, the requirements, threat model, and relevant architecture
decision before changing protection or recovery behavior.

## Setup

Use Windows x64 and the .NET 10 SDK for the desktop prototype. Kernel development
later requires a matching supported Visual Studio/SDK/WDK toolchain and an isolated
Windows VM. Do not enable driver test signing on a personal production computer.

Run the commands in README to build and execute validation. Keep generated
executables, recovery vaults, credentials, and real incident samples out of Git.

## Branches and review

Use `feature/<short-purpose>`, `fix/<short-purpose>`, or `docs/<short-purpose>`.
Open one focused PR against `main`. Describe the resulting behavior, affected
requirements, migration/security implications, and validation evidence.
Do not claim a capability solely because a button or interface exists.

Merge by squash after checks pass and the change has been reviewed. Releases
come from tagged `main`, not a workstation's uncommitted files. See the exact
commands and bootstrap exception in [Git workflow](docs/git-workflow.md).

## Code expectations

- Keep UI, recovery storage, and scan execution independently testable.
- Use cancellation and bounded queues for long operations.
- Do not turn scanner failures into clean results.
- Do not automatically delete suspicious files or overwrite the only good version.
- Refuse unsafe paths and reparse-point traversal in recovery operations.
- Surface storage exhaustion, unsupported files, and monitoring interruptions.
- Never log document contents, credentials, or arbitrary command output publicly.
- Explain behavior changes in CHANGELOG and update installation/user instructions.

## Tests

Changes affecting stored bytes require corruption, deletion, overwrite, collision,
and restore integrity scenarios. Permission changes require denied-access tests.
Driver changes need VM crash/reboot/disconnection tests and Driver Verifier.
Remote commands need authorization, tenant isolation, replay, and expiration tests.

## Contributions and licensing

A project license has not been selected. Agree licensing with the maintainer
before accepting outside contributions or distributing third-party assets.
