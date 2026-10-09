# Feature → pull request → merge workflow

## Empty-repository bootstrap

An empty remote has no base branch for a PR. Establish `main` with documentation
and project conventions in one initial commit. This is the only bootstrap direct
push; subsequent implementation uses feature branches and PRs.

```powershell
git add README.md CONTRIBUTING.md CHANGELOG.md SECURITY.md AGENTS.md PROJECT_MEMORY.md .gitignore docs
git commit -m "docs: establish KevlarEDR design and development workflow"
git push -u origin main
```

Stage named files. Never blindly stage local connector configuration or credentials.
If `main` already exists, fetch and integrate it before bootstrap; never force-push.

## Feature work

```powershell
git switch main
git pull --ff-only
git switch -c feature/local-desktop-app
# Implement and update tests, changelog, and user documentation.
dotnet build KevlarEDR.slnx -c Release
dotnet run --project tests/KevlarEDR.Validation -c Release
git diff --check
git add src tests scripts .github KevlarEDR.slnx global.json docs CHANGELOG.md README.md
git commit -m "feat: add local Windows scanning and file version recovery"
git push -u origin feature/local-desktop-app
gh pr create --base main --head feature/local-desktop-app --title "Add local Windows scanning and file recovery" --body-file pr-description.md
```

The PR description must describe actual behavior, limits, validation, and package
location. The temporary body file does not need to be committed.

## Review and merge

Wait for Windows CI, examine the diff, and resolve genuine review findings. Use
squash merge once the branch is ready and reviewed. Require status checks and PR
review through repository rules where the GitHub plan permits; until configured,
this policy is a convention rather than an enforced guarantee.

```powershell
gh pr checks <number> --watch
gh pr merge <number> --squash --delete-branch
git switch main
git pull --ff-only
```

Never merge a failing build to deliver faster. Avoid forced remote-history rewrites.
If a PR depends on another, identify its base/dependency explicitly.

## Release

Tag a verified commit on `main`; release workflow packages that exact revision.
Publish release notes, checksums, supported OS/architecture, actual limitations,
and installation instructions. A published release must not contain recovery
vaults, personal documents, private keys, crash dumps, or local MCP configuration.
