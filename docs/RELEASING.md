# Releasing Notty

Notty's releases are automated by [`.github/workflows/release.yml`](../.github/workflows/release.yml).
Pushing a version tag (`v*`) builds a **self-contained, single-file Windows binary** and publishes
it to a GitHub Release. Creating a release is therefore just: **bump → changelog → commit → tag → push**.

## Steps

Example: releasing `1.1.0`.

### 1. Choose the version (SemVer)

| Bump | Example | When |
|------|---------|------|
| **patch** | `1.0.1` | Bug fixes only |
| **minor** | `1.1.0` | New features, backward-compatible |
| **major** | `2.0.0` | Breaking changes |

### 2. Bump `<Version>` in the project

In [`src/Notty.App/Notty.App.csproj`](../src/Notty.App/Notty.App.csproj):

```xml
<Version>1.1.0</Version>
```

This drives the executable's file version and the **About** box. Keep it in sync with the tag.

### 3. Update the changelog

Add a section at the top of [`CHANGELOG.md`](../CHANGELOG.md):

```markdown
## [1.1.0] - 2026-07-15

### Added
- ...

### Fixed
- ...

[1.1.0]: https://github.com/Blankscreen-exe/Notty-Text-Editor/releases/tag/v1.1.0
```

### 4. Commit the prep

```bash
git add src/Notty.App/Notty.App.csproj CHANGELOG.md
git commit -m "chore(release): prepare v1.1.0"
```

### 5. Tag and push

This is what actually triggers the build and publishes the release:

```bash
git tag -a v1.1.0 -m "Notty v1.1.0"
git push origin main
git push origin v1.1.0
```

Watch the **Actions** tab. Within a few minutes the **Releases** page will have `Notty.exe`
and a zip, and the README download links/badge update automatically.

## Rules to remember

- **The tag must start with `v`** (e.g. `v1.1.0`). A tag like `1.1.0` will not trigger the workflow.
- **Keep the csproj `<Version>` and the tag in sync.** The tag drives the release name and asset
  names; the csproj version drives the embedded file version and About box.
- **One tag = one release.** To fix a broken build, push a follow-up patch (`v1.1.1`), or delete the
  tag/release and re-push.

## Fixing a botched release

```bash
git tag -d v1.1.0                  # delete local tag
git push --delete origin v1.1.0    # delete remote tag
# delete the failed Release on GitHub, fix the issue, then re-tag the correct commit
```

## What the workflow produces

- `Notty.exe` — self-contained, single-file, `win-x64`. No .NET install needed on the user's machine.
- `Notty-<version>-win-x64.zip` — the same executable, zipped.
- Auto-generated release notes from the commits since the previous tag.

> **Note:** the binary is unsigned, so Windows SmartScreen shows an "unknown publisher" prompt on
> first run. This is expected for unsigned builds; users can choose *More info → Run anyway*. A code-
> signing certificate would remove the warning.
