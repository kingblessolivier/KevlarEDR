# Release and deployment design

Initial target: Windows x64, per-user installation, self-contained .NET desktop
executable. Do not install a driver or persistent service in version 0.1.

## Build outputs

- Published executable and accompanying license/notices where required.
- Versioned portable archive.
- Per-user setup executable when the packaging tool is available.
- SHA-256 checksums and sanitized validation evidence.

Use a supported installer tool, default to `%LOCALAPPDATA%\Programs\KevlarEDR`,
and create a Start Menu shortcut. Uninstall app files while preserving
`%LOCALAPPDATA%\KevlarEDR` recovery data. Any future data deletion must be explicit.
Do not auto-start scans or enable privileged policy during installation.

## Signing and updates

The prototype may be unsigned; label this accurately. Checksums verify byte
matching, not publisher identity. A production release needs Authenticode signing,
certificate/key management, trusted distribution, and staged updates. Do not
instruct users to disable Windows security protections to bypass warnings.

Future privileged service/driver installation requires administrative installation,
proper driver signing, upgrade rollback, and VM validation. It must not inherit
the prototype's per-user installer assumptions.

## Automation

PR CI builds and validates; a tag-based release workflow packages and uploads
artifacts from the tagged commit. Grant the minimum GitHub token permissions;
write access is needed only for release publishing. Avoid untrusted PR code in
privileged workflow contexts such as `pull_request_target`.

## Platform references

- [Self-contained single-file publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)
- [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/)
- [Inno Setup installer directives](https://jrsoftware.org/ishelp/topic_setupsection.htm)
- [Windows driver signing](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/kernel-mode-code-signing-policy--windows-vista-and-later-)
