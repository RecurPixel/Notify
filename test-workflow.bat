@echo off
REM Test the NuGet publish workflow locally using Docker

setlocal enabledelayedexpansion

echo.
echo 🐳 Testing RecurPixel.Notify NuGet publish workflow locally
echo ===========================================================
echo.

REM Check if Docker is installed
docker --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker not found. Please install Docker Desktop.
    exit /b 1
)

echo 📦 Building Docker test image...
docker build -f Dockerfile.test -t recurpixel-notify-test:latest .

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Docker build successful!
    echo.
    echo � Workflow validation complete. Ready to publish!
    echo.
    echo 📝 Next Steps:
    echo   1. Review package output above
    echo   2. Commit changes: git commit -m "chore: v0.2.0 stable release"
    echo   3. Push to GitHub: git push origin main
    echo   4. Create release tag on GitHub for v0.2.0
    echo   5. GitHub Actions will automatically publish all 35 packages to NuGet
    echo.
    echo 📋 In case of package issues, inspect the Docker image:
    echo    docker run --rm -v "%cd%\nupkgs":/workspace/nupkgs recurpixel-notify-test:latest ls -lah nupkgs/
) else (
    echo.
    echo ❌ Docker build failed. Check errors above.
    exit /b 1
)
