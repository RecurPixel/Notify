<#
.SYNOPSIS
    Publishes all RecurPixel.Notify packages to NuGet.org in dependency order.

.DESCRIPTION
    Packs and pushes all 34 packages in three tiers:
      Tier 0: Core (no dependencies)
      Tier 1: Orchestrator + all 31 adapters (depend on Core)
      Tier 2: SDK meta-package (depends on everything)

    Each tier waits for NuGet indexing before proceeding to the next.

.PARAMETER ApiKey
    NuGet.org API key. Required unless -PackOnly is set.

.PARAMETER Source
    NuGet source URL. Defaults to https://api.nuget.org/v3/index.json

.PARAMETER PackOnly
    Only pack the packages into ./nupkgs without pushing.

.PARAMETER SkipPack
    Skip packing and push existing .nupkg files from ./nupkgs.

.PARAMETER SkipBuild
    Skip the pre-publish build + test step.

.PARAMETER IndexWaitSeconds
    Seconds to wait between tiers for NuGet indexing. Default: 120

.EXAMPLE
    .\publish-nuget.ps1 -ApiKey "your-key-here"

.EXAMPLE
    .\publish-nuget.ps1 -PackOnly

.EXAMPLE
    .\publish-nuget.ps1 -ApiKey "your-key-here" -SkipPack -IndexWaitSeconds 60
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$ApiKey = "abcdef1234567890", # Placeholder value to avoid empty string. Must be overridden when pushing.

    [Parameter()]
    [string]$Source = "https://api.nuget.org/v3/index.json",

    [switch]$PackOnly,
    [switch]$SkipPack,
    [switch]$SkipBuild,

    [int]$IndexWaitSeconds = 120
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Configuration ---

$SolutionRoot = $PSScriptRoot
$OutputDir    = Join-Path $SolutionRoot "nupkgs"
$SrcDir       = Join-Path $SolutionRoot "src"
$Configuration = "Release"

# Read version from Directory.Build.props
[xml]$buildProps = Get-Content (Join-Path $SolutionRoot "Directory.Build.props")
$Version = $buildProps.Project.PropertyGroup[1].Version
if (-not $Version) {
    Write-Error "Could not read Version from Directory.Build.props"
    exit 1
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  RecurPixel.Notify NuGet Publisher"           -ForegroundColor Cyan
Write-Host "  Version: $Version"                            -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# --- Validation ---

if (-not $PackOnly -and -not $ApiKey) {
    Write-Error "ApiKey is required when pushing to NuGet. Use -PackOnly to only create packages."
    exit 1
}

# --- Package tiers (dependency order) ---

$Tier0 = @(
    "RecurPixel.Notify.Core"
)

$Tier1 = @(
    # Orchestrator
    "RecurPixel.Notify.Orchestrator"

    # Email adapters
    "RecurPixel.Notify.Email.SendGrid"
    "RecurPixel.Notify.Email.Smtp"
    "RecurPixel.Notify.Email.Mailgun"
    "RecurPixel.Notify.Email.Postmark"
    "RecurPixel.Notify.Email.Resend"
    "RecurPixel.Notify.Email.AwsSes"
    "RecurPixel.Notify.Email.AzureCommEmail"

    # SMS adapters
    "RecurPixel.Notify.Sms.Twilio"
    "RecurPixel.Notify.Sms.MessageBird"
    "RecurPixel.Notify.Sms.Plivo"
    "RecurPixel.Notify.Sms.Sinch"
    "RecurPixel.Notify.Sms.Vonage"
    "RecurPixel.Notify.Sms.AwsSns"
    "RecurPixel.Notify.Sms.AzureCommSms"

    # Push adapters
    "RecurPixel.Notify.Push.Fcm"
    "RecurPixel.Notify.Push.Apns"
    "RecurPixel.Notify.Push.Expo"
    "RecurPixel.Notify.Push.OneSignal"

    # WhatsApp adapters
    "RecurPixel.Notify.WhatsApp.Twilio"
    "RecurPixel.Notify.WhatsApp.MetaCloud"
    "RecurPixel.Notify.WhatsApp.Vonage"

    # Chat and Social adapters
    "RecurPixel.Notify.Slack"
    "RecurPixel.Notify.Discord"
    "RecurPixel.Notify.Teams"
    "RecurPixel.Notify.Telegram"
    "RecurPixel.Notify.Facebook"
    "RecurPixel.Notify.Line"
    "RecurPixel.Notify.Viber"
    "RecurPixel.Notify.Mattermost"
    "RecurPixel.Notify.RocketChat"

    # Other
    "RecurPixel.Notify.InApp"
)

$Tier2 = @(
    "RecurPixel.Notify.Sdk"
)

$AllPackages = $Tier0 + $Tier1 + $Tier2

# --- Helper functions ---

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "--- $Message ---" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "  [FAIL] $Message" -ForegroundColor Red
}

function Wait-ForIndexing {
    param([int]$Seconds, [string]$TierName)

    Write-Host ""
    Write-Host "  Waiting $Seconds seconds for NuGet to index $TierName packages..." -ForegroundColor Magenta

    for ($i = $Seconds; $i -gt 0; $i -= 10) {
        $remaining = [Math]::Min($i, 10)
        Write-Host "    $i seconds remaining..." -ForegroundColor DarkGray
        Start-Sleep -Seconds $remaining
    }

    Write-Host "  Indexing wait complete." -ForegroundColor Magenta
}

# --- Step 1: Build and test ---

if (-not $SkipBuild) {
    Write-Step "Building solution (Release)"
    dotnet build (Join-Path $SolutionRoot "RecurPixel.Notify.sln") -c $Configuration --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Build failed. Fix errors before publishing."
        exit 1
    }
    Write-Success "Build succeeded"

    Write-Step "Running tests"
    dotnet test (Join-Path $SolutionRoot "RecurPixel.Notify.sln") -c $Configuration --nologo --no-build -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Tests failed. Fix failures before publishing."
        exit 1
    }
    Write-Success "All tests passed"
}

# --- Step 2: Pack ---

if (-not $SkipPack) {
    Write-Step "Packing $($AllPackages.Count) packages (v$Version)"

    # Clean output directory
    if (Test-Path $OutputDir) {
        Remove-Item $OutputDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    $packFailed = @()

    foreach ($pkg in $AllPackages) {
        $csproj = Join-Path $SrcDir "$pkg\$pkg.csproj"

        if (-not (Test-Path $csproj)) {
            Write-Failure "$pkg -- csproj not found at $csproj"
            $packFailed += $pkg
            continue
        }

        dotnet pack $csproj -c $Configuration --no-build --output $OutputDir --nologo -v quiet 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            # Retry with build (in case --no-build fails for multi-target)
            dotnet pack $csproj -c $Configuration --output $OutputDir --nologo -v quiet
            if ($LASTEXITCODE -ne 0) {
                Write-Failure "$pkg"
                $packFailed += $pkg
                continue
            }
        }
        Write-Success "$pkg"
    }

    if ($packFailed.Count -gt 0) {
        Write-Host ""
        Write-Failure "Failed to pack $($packFailed.Count) package(s): $($packFailed -join ', ')"
        exit 1
    }

    # Summary
    $nupkgCount = (Get-ChildItem $OutputDir -Filter "*.nupkg").Count
    $snupkgCount = (Get-ChildItem $OutputDir -Filter "*.snupkg").Count
    Write-Host ""
    Write-Host "  Packed $nupkgCount .nupkg + $snupkgCount .snupkg files in ./nupkgs/" -ForegroundColor Cyan
}

if ($PackOnly) {
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Green
    Write-Host "  Pack complete. Skipping push (-PackOnly)."   -ForegroundColor Green
    Write-Host "=============================================" -ForegroundColor Green
    exit 0
}

# --- Step 3: Push in dependency order ---

function Push-Tier {
    param(
        [string]$TierName,
        [string[]]$Packages
    )

    Write-Step "Publishing $TierName ($($Packages.Count) packages)"

    $pushFailed = @()

    foreach ($pkg in $Packages) {
        $nupkg = Get-ChildItem $OutputDir -Filter "$pkg.$Version.nupkg" | Select-Object -First 1

        if (-not $nupkg) {
            Write-Failure "$pkg -- .nupkg not found in $OutputDir"
            $pushFailed += $pkg
            continue
        }

        dotnet nuget push $nupkg.FullName --api-key $ApiKey --source $Source --skip-duplicate --no-symbols 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Failure "$pkg"
            $pushFailed += $pkg
            continue
        }
        Write-Success "$pkg"

        # Push symbol package if it exists
        $snupkg = Get-ChildItem $OutputDir -Filter "$pkg.$Version.snupkg" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($snupkg) {
            dotnet nuget push $snupkg.FullName --api-key $ApiKey --source $Source --skip-duplicate 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "$pkg (symbols)"
            }
            # Symbol push failure is non-fatal
        }
    }

    if ($pushFailed.Count -gt 0) {
        Write-Host ""
        Write-Failure "Failed to push $($pushFailed.Count) package(s): $($pushFailed -join ', ')"
        Write-Host "  Fix the issue and re-run with -SkipPack to retry push only." -ForegroundColor Yellow
        exit 1
    }
}

# Tier 0: Core
Push-Tier -TierName "Tier 0 - Core" -Packages $Tier0
Wait-ForIndexing -Seconds $IndexWaitSeconds -TierName "Core"

# Tier 1: Orchestrator + all adapters
Push-Tier -TierName "Tier 1 - Orchestrator + Adapters" -Packages $Tier1
Wait-ForIndexing -Seconds $IndexWaitSeconds -TierName "Tier 1"

# Tier 2: SDK meta-package
Push-Tier -TierName "Tier 2 - SDK Meta-package" -Packages $Tier2

# --- Done ---

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  All $($AllPackages.Count) packages published successfully!"  -ForegroundColor Green
Write-Host "  Version: $Version"                                            -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Verify at: https://www.nuget.org/profiles/RecurPixel" -ForegroundColor Cyan
Write-Host ""
