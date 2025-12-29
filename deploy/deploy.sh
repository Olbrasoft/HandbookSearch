#!/bin/bash
set -e

DEPLOY_DIR="/opt/olbrasoft/handbook-search"
PROJECT_DIR="$HOME/Olbrasoft/HandbookSearch"
CLI_PROJECT="$PROJECT_DIR/src/HandbookSearch.Cli/HandbookSearch.Cli.csproj"

echo "Building HandbookSearch.Cli..."
cd "$PROJECT_DIR"
dotnet build -c Release

echo "Running tests..."
dotnet test --verbosity minimal
if [ $? -ne 0 ]; then
    echo "❌ Tests failed! Deployment aborted."
    exit 1
fi

echo "Publishing CLI..."
dotnet publish "$CLI_PROJECT" \
    -c Release \
    -o "$DEPLOY_DIR/cli/" \
    --no-self-contained

echo "Creating directory structure..."
sudo mkdir -p "$DEPLOY_DIR/logs"
sudo chown -R $USER:$USER "$DEPLOY_DIR"

echo "Setting permissions..."
chmod +x "$DEPLOY_DIR/cli/HandbookSearch.Cli"

echo "✅ Deployment complete!"
echo "CLI location: $DEPLOY_DIR/cli/HandbookSearch.Cli"
echo ""
echo "Usage examples:"
echo "  $DEPLOY_DIR/cli/HandbookSearch.Cli import-files --files \"path.md\" --translate-cs"
echo "  $DEPLOY_DIR/cli/HandbookSearch.Cli delete-files --files \"path.md\""
