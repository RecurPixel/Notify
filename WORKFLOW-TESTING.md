# Workflow Testing Guide

## Local NuGet Publish Workflow Validation

Test the GitHub Actions workflow **locally in Docker** before pushing to GitHub and creating a release.

### What Gets Tested

- ✅ `.NET dependency restoration`
- ✅ `Release build` with all 35 projects
- ✅ `Unit tests` execution
- ✅ **All 35 NuGet packages are created** (including the critical Sdk meta-package)
- ✅ Package naming and versioning
- ✅ Meta-package configuration (`<IncludeBuildOutput>`, `<IsPackable>`)

### Prerequisites

- **Docker Desktop** (Windows/Mac) or **Docker CLI** (Linux)
  - [Install Docker Desktop](https://www.docker.com/products/docker-desktop)

### How to Run

#### **Windows**
```cmd
.\test-workflow.bat
```

#### **macOS / Linux**
```bash
chmod +x test-workflow.sh
./test-workflow.sh
```

### What Happens

1. **Builds** a Docker image based on Ubuntu with .NET 8 SDK
2. **Restores** NuGet dependencies
3. **Builds** the entire solution in Release mode
4. **Runs** all unit tests
5. **Packs** NuGet packages to `./nupkgs/`
6. **Validates** that exactly 35 `.nupkg` files were created
7. **Reports** final package list and total size

### Expected Output

```
📦 Checking packed packages...
Total .nupkg files: 35
✅ All 35 packages present!

📋 Final Package List:
     1 RecurPixel.Notify.Core.0.2.0.nupkg
     2 RecurPixel.Notify.Orchestrator.0.2.0.nupkg
     3 RecurPixel.Notify.0.2.0.nupkg
     4 RecurPixel.Notify.Email.SendGrid.0.2.0.nupkg
     5 RecurPixel.Notify.Email.Smtp.0.2.0.nupkg
     ...
    35 RecurPixel.Notify.Sdk.0.2.0.nupkg

✅ Docker build successful!
📤 Workflow validation complete. Ready to publish!
```

### If Tests Fail

**Missing packages?**
- Check the `.csproj` files for `<IsPackable>true</IsPackable>` property
- Verify project references in SDK meta-packages are correct
- Run `dotnet list nuget-package` to inspect dependencies

**Build errors?**
- Verify .NET 8 SDK is installed locally: `dotnet --version`
- Check that all projects compile: `dotnet build --configuration Release`

**Test failures?**
- Run tests locally first: `dotnet test --configuration Release`
- Check GitHub Actions workflow logs for integration test requirements

### Inspecting Packages

After a successful test, inspect the generated packages:

**List packages:**
```bash
docker run --rm -v $(pwd)/nupkgs:/workspace/nupkgs recurpixel-notify-test:latest ls -lah nupkgs/
```

**Extract and inspect a package:**
```bash
cd nupkgs
unzip RecurPixel.Notify.Sdk.0.2.0.nupkg -d RecurPixel.Notify.Sdk.inspect
cat RecurPixel.Notify.Sdk.inspect/.nuspec
```

### Pre-Push Checklist

- [ ] Run `./test-workflow.bat` (or `.sh`)
- [ ] All 35 packages created successfully
- [ ] No build or test errors
- [ ] Version number matches your release tag (e.g., `0.2.0`)
- [ ] Review `./nupkgs/` directory for size anomalies

✅ **Then:** Push to GitHub and create a release.

### GitHub Actions Workflow File

The actual CI/CD workflow is defined in:
- [.github/workflows/publish-nuget.yml](../.github/workflows/publish-nuget.yml)

It uses:
- 5-tier dependency ordering (Core → Orchestrator → Main → Adapters → SDK)
- 120s waits between tiers for NuGet indexing
- Automated GitHub Release asset uploads

---

**Questions?**  
See [PUBLISHING.md](./PUBLISHING.md) for the full release guide.
