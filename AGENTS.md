# Unity Project Automation Rules

## Ownership

- Files copied from RemoteDevLoop are owned by the consuming project after the
  copy. Review and version their changes in this project.
- Keep `Tools/Agent`, `Assets/Editor/Build`, and `.github/workflows` project
  specific. Do not load mutable build logic from another repository at runtime.

## Build loop

- Run `Tools/Agent/Run-Tests.ps1` before `Tools/Agent/Build-Android.ps1`.
- Supply Android version name, version code, and output path explicitly.
- Keep release creation, artifact distribution, and credential access outside
  Unity editor build code.
- Never commit secrets, GitHub tokens, Firebase service accounts, Android
  keystores, passwords, or local credential configuration files.
- Do not use legacy release scripts that read credential configuration files
  unless their security and ownership have been explicitly reviewed.

## Self-hosted runner

- Use self-hosted workflows only in private repositories with trusted writers.
- Do not add `pull_request` triggers to self-hosted runner workflows without an
  explicit security review.
- Preserve logs, test results, and build reports when a job fails.
