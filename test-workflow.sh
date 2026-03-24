#!/bin/bash
# Test the NuGet publish workflow locally using Docker

set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

echo "🐳 Testing RecurPixel.Notify NuGet publish workflow locally"
echo "==========================================================="
echo ""

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker Desktop or Docker CLI."
    exit 1
fi

echo "📦 Building Docker test image..."
docker build -f Dockerfile.test -t recurpixel-notify-test:latest .

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Docker build successful!"
    echo ""
    echo "📋 To inspect the generated packages, run:"
    echo "   docker run --rm -v $(pwd)/nupkgs:/workspace/nupkgs recurpixel-notify-test:latest ls -lah nupkgs/"
    echo ""
    echo "📤 Workflow validation complete. Ready to publish!"
    echo ""
    echo "📝 Next Steps:"
    echo "  1. Review package output above"
    echo "  2. Commit changes: git commit -m 'chore: v0.2.0 stable release'"
    echo "  3. Push to GitHub: git push origin main"
    echo "  4. Create release tag on GitHub for v0.2.0"
    echo "  5. GitHub Actions will automatically publish all 35 packages to NuGet"
    echo ""
    echo "📋 In case of package issues, inspect the Docker image:"
    echo "   docker run --rm -v $(pwd)/nupkgs:/workspace/nupkgs recurpixel-notify-test:latest ls -lah nupkgs/"
else
    echo ""
    echo "❌ Docker build failed. Check errors above."
    exit 1
fi
