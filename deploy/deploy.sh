#!/bin/bash
set -e

DEPLOY_DIR="/opt/olbrasoft/handbook-search"
PROJECT_DIR="$HOME/Olbrasoft/HandbookSearch"
CLI_PROJECT="$PROJECT_DIR/src/HandbookSearch.Cli/HandbookSearch.Cli.csproj"
SECRETS_DIR="$HOME/.config/handbook-search/secrets"
KEYS_DIR="$HOME/.config/handbook-search/keys"

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

# Ensure SecureStore directories exist
if [ ! -d "$SECRETS_DIR" ]; then
    mkdir -p "$SECRETS_DIR"
    echo "📁 Created secrets directory: $SECRETS_DIR"
fi

if [ ! -d "$KEYS_DIR" ]; then
    mkdir -p "$KEYS_DIR"
    echo "📁 Created keys directory: $KEYS_DIR"
fi

# Check SecureStore vault and key
VAULT_MISSING=false
KEY_MISSING=false

if [ ! -f "$SECRETS_DIR/secrets.json" ]; then
    VAULT_MISSING=true
fi

if [ ! -f "$KEYS_DIR/secrets.key" ]; then
    KEY_MISSING=true
fi

if [ "$VAULT_MISSING" = true ] || [ "$KEY_MISSING" = true ]; then
    echo ""
    echo "⚠️  WARNING: SecureStore configuration incomplete!"
    if [ "$VAULT_MISSING" = true ]; then
        echo "   - Vault not found: $SECRETS_DIR/secrets.json"
    fi
    if [ "$KEY_MISSING" = true ]; then
        echo "   - Key not found: $KEYS_DIR/secrets.key"
    fi
    echo ""
    echo "   Create vault with:"
    echo "   SecureStore create -s $SECRETS_DIR/secrets.json -k $KEYS_DIR/secrets.key"
    echo ""
    echo "   Then add secrets:"
    echo "   SecureStore set -s $SECRETS_DIR/secrets.json -k $KEYS_DIR/secrets.key \"Database:Password=your_password\""
    echo "   SecureStore set -s $SECRETS_DIR/secrets.json -k $KEYS_DIR/secrets.key \"AzureTranslator:ApiKey=your_api_key\""
fi

echo ""
echo "✅ Deployment complete!"
echo "CLI location: $DEPLOY_DIR/cli/HandbookSearch.Cli"
echo ""
echo "Usage examples:"
echo "  $DEPLOY_DIR/cli/HandbookSearch.Cli import-files --files \"path.md\" --translate-cs"
echo "  $DEPLOY_DIR/cli/HandbookSearch.Cli delete-files --files \"path.md\""
