# Self-Hosted Runner Setup

Instructions for setting up the self-hosted GitHub Actions runner for HandbookSearch deployment.

## Prerequisites

- Debian/Ubuntu Linux
- .NET 10 SDK
- PostgreSQL 16+ with pgvector extension
- Ollama with qwen3-embedding:0.6b model

## Runner Installation

1. Follow [GitHub's official guide](https://docs.github.com/en/actions/hosting-your-own-runners/adding-self-hosted-runners)
2. Use label: `handbook-search`

## SecureStore Configuration

HandbookSearch uses SecureStore for encrypted secrets management.

### 1. Install SecureStore CLI

```bash
dotnet tool install --global SecureStore.Client
```

### 2. Create Vault

```bash
mkdir -p ~/.config/handbook-search/{secrets,keys}

SecureStore create \
  -s ~/.config/handbook-search/secrets/secrets.json \
  -k ~/.config/handbook-search/keys/secrets.key

chmod 600 ~/.config/handbook-search/keys/secrets.key
```

### 3. Add Secrets

```bash
SECRETS=~/.config/handbook-search/secrets/secrets.json
KEY=~/.config/handbook-search/keys/secrets.key

# Database password (if PostgreSQL requires authentication)
SecureStore set -s $SECRETS -k $KEY "Database:Password=your_postgres_password"

# Azure Translator (for Czech embeddings with --translate-cs flag)
SecureStore set -s $SECRETS -k $KEY "AzureTranslator:ApiKey=your_api_key"
SecureStore set -s $SECRETS -k $KEY "AzureTranslator:FallbackApiKey=your_fallback_key"
```

### 4. Verify Setup

```bash
# Check vault exists
ls -la ~/.config/handbook-search/secrets/secrets.json

# Test CLI with secrets
cd ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Cli
dotnet run -- import-all --path ~/Olbrasoft/engineering-handbook
```

## Secrets Reference

| Secret | Description | Required |
|--------|-------------|----------|
| `Database:Password` | PostgreSQL password | If auth required |
| `AzureTranslator:ApiKey` | Azure Translator API key | For `--translate-cs` |
| `AzureTranslator:FallbackApiKey` | Fallback API key | Optional |

## Security Notes

- Never commit `secrets.key` to git
- Set restrictive permissions: `chmod 600 ~/.config/handbook-search/keys/secrets.key`
- Vault is encrypted with AES + HMAC
- Keep backup of keyfile in secure location

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "SecureStore vault not found" | Run SecureStore create command |
| "Failed to load secrets" | Check keyfile permissions and path |
| "Database connection failed" | Verify Database:Password in SecureStore |
| "Azure Translator 401" | Check AzureTranslator:ApiKey |
