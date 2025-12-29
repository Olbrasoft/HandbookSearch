# HandbookSearch Translation CLI

Batch translation tool for translating engineering handbook markdown files using Azure Translator API.

## Features

- Translates all markdown files from source to target directory
- Preserves directory structure
- Adds AI agents marker to translated files
- Built-in rate limiting (555 chars/second with 20% safety margin)
- Time estimation before translation
- Progress tracking with emoji indicators

## Setup

### 1. Configure Azure Translator Secrets

```bash
cd ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Translation.Cli

# Set API key
dotnet user-secrets set "AzureTranslator:ApiKey" "YOUR_API_KEY"

# Set region (e.g., "westeurope")
dotnet user-secrets set "AzureTranslator:Region" "YOUR_REGION"
```

### 2. Verify Configuration

Check that secrets are set:
```bash
dotnet user-secrets list
```

## Usage

### Translate All Files

```bash
dotnet run -- translate-all \
  --source ~/GitHub/Olbrasoft/engineering-handbook \
  --target ~/GitHub/Olbrasoft/engineering-handbook-cs \
  --target-lang cs
```

Options:
- `--source` (required): Source handbook directory
- `--target` (required): Target directory for translated files
- `--target-lang` (optional): Target language code (default: "cs")

## Example Output

```
📄 Found 42 markdown files
⏱️  Estimated time: 8 minutes (approximately)

🔄 Starting translation...

📝 README.md... ✅
📝 development-guidelines/workflow-guide.md... ✅
📝 solid-principles/solid-principles-2025.md... ✅
...

✅ Translation completed!
   Translated: 42
   Errors:     0
   Total:      42
```

## Rate Limiting

The tool automatically calculates delay based on text length:
- Formula: `delay = (charCount / 555) * 1.2`
- Minimum delay: 200ms per request
- Respects Azure Translator free tier limit (2M chars/month)

## Translated File Format

Each translated file includes a marker for AI agents:
```markdown
<!-- AI_AGENTS_IGNORE: This is a Czech translation for embedding search only. Agents should use the English version. -->

[Translated content...]
```
