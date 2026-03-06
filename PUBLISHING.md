# Publishing Releases

RecurPixel.Notify uses GitHub Actions to automatically publish to NuGet when you create a Release.

## Prerequisites

1. **NuGet API Key** — stored as a GitHub secret
2. **GitHub token** — automatic (included in every Action run)

## Setup (One-time)

### 1. Add NuGet API Key to GitHub Secrets

1. Go to: **Settings → Secrets and variables → Actions**
2. Click **New repository secret**
3. Name: `NUGET_API_KEY`
4. Value: Your NuGet.org API key (from https://www.nuget.org/account/api-keys)
5. Click **Add secret**

That's it! The workflow will use this key to push all packages.

---

## Publishing a Release

### Step 1: Create a Git tag

```bash
git tag v0.2.0-beta.1
git push origin v0.2.0-beta.1
```

Or update `Directory.Build.props` first:
```xml
<Version>0.2.0-beta.1</Version>
```

### Step 2: Create GitHub Release

1. Go to: **Code → Releases → Draft a new release**
2. **Choose a tag**: select `v0.2.0-beta.1`
3. **Release title**: `v0.2.0-beta.1`
4. **Describe this release**: Copy from [CHANGELOG.md](CHANGELOG.md)
5. **Pre-release**: Check this for beta/alpha versions
6. Click **Publish release**

### Step 3: Watch the Action

The workflow automatically:
1. ✅ Checks out code
2. ✅ Builds solution (Release)
3. ✅ Runs all tests
4. ✅ Packs 35 NuGet packages
5. ✅ Pushes to NuGet.org
6. ✅ Attaches packages as release assets

**Logs:** Go to **Actions → Publish to NuGet → [Latest run]** to see real-time progress.

---

## What Gets Published

All packages in one go:
- **Tier 0:** `RecurPixel.Notify.Core`
- **Tier 1:** `RecurPixel.Notify.Orchestrator`
- **Tier 2:** `RecurPixel.Notify` (meta-package)
- **Tier 3:** 31 adapters (Email, SMS, Push, WhatsApp, Chat, Social, InApp)
- **Tier 4:** `RecurPixel.Notify.Sdk` (meta-package)

Each package includes both `.nupkg` and `.snupkg` (symbol packages).

---

## After Publishing

1. **Verify on NuGet** — https://www.nuget.org/packages?q=RecurPixel.Notify
2. **Check Release Assets** — GitHub release page includes all `.nupkg` files
3. **Announce** — update documentation, social media, etc.

---

## Troubleshooting

### Action fails with "API key not found"
Check that `NUGET_API_KEY` secret is set correctly:
1. Settings → Secrets and variables → Actions
2. Verify `NUGET_API_KEY` exists and has value

### Packages already published
The action includes `--skip-duplicate`, so re-running won't cause errors.

### Manual Fallback
If the action fails unexpectedly, you can still use the local script:
```powershell
.\publish-nuget.ps1 -ApiKey "your-key" -SkipBuild
```

---

## Workflow File

The workflow is defined in [.github/workflows/publish-nuget.yml](.github/workflows/publish-nuget.yml).

**Triggers on:** Release published (not on tag alone)
**Runs on:** Ubuntu latest
**Duration:** ~5-10 minutes
