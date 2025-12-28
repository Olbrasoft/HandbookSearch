# HandbookSearch Web UI

Simple web interface for searching engineering handbook documents using semantic search.

## Features

- **Semantic Search**: Search in Czech or English using AI-powered embeddings
- **Clean UI**: Modern, responsive design with Tailwind CSS
- **Direct Links**: Results link directly to GitHub files (no local content storage)
- **Score Visualization**: Color-coded similarity scores
- **Advanced Options**: Configurable result limit and max distance threshold
- **Copy Path**: Quick clipboard copy for file paths

## Quick Start

### Prerequisites

1. **API Server must be running**:
   ```bash
   cd ~/Olbrasoft/HandbookSearch
   dotnet run --project src/HandbookSearch.AspNetCore.Api
   ```
   API runs on: `http://localhost:5170`

2. **Database populated** with engineering-handbook documents

### Running the Web UI

**Option 1: Python HTTP Server (Recommended)**
```bash
cd ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Web
python3 -m http.server 3000
```
Open: `http://localhost:3000`

**Option 2: PHP Server**
```bash
cd ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Web
php -S localhost:3000
```

**Option 3: Node.js HTTP Server**
```bash
npm install -g http-server
cd ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Web
http-server -p 3000
```

**Option 4: Open directly in browser**
```bash
firefox ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Web/index.html
# or
google-chrome ~/Olbrasoft/HandbookSearch/src/HandbookSearch.Web/index.html
```
Note: CORS might block API calls when opening file:// URLs. Use HTTP server instead.

## Usage

1. Enter search query in Czech or English (e.g., "git workflow", "SOLID principy")
2. Optionally adjust:
   - **Limit**: Number of results (1-100, default: 10)
   - **Max Distance**: Maximum cosine distance threshold (0-2, lower = more similar)
3. Click **Search** or press Enter
4. Click result titles to open files in GitHub
5. Click **Copy path** to copy file path to clipboard

## Search Examples

| Query | Expected Results |
|-------|------------------|
| `git workflow` | Git branching strategies, commit guidelines |
| `SOLID principy` | SOLID principles documentation |
| `testování` | Testing best practices |
| `dependency injection` | DI patterns, IoC containers |
| `code review` | Code review checklists |

## Score Interpretation

| Score | Color | Meaning |
|-------|-------|---------|
| ≥ 70% | Green | Highly relevant |
| 50-69% | Yellow | Moderately relevant |
| < 50% | Red | Low relevance |

Score is calculated as: `score = 1 - distance` (0-1 range, shown as percentage)

## Architecture

**Static HTML + Vanilla JavaScript**
- No build process required
- No dependencies (Tailwind via CDN)
- Single `index.html` file
- Direct API calls using Fetch API

**API Integration**
```
GET http://localhost:5170/api/search?q={query}&limit={limit}&maxDistance={maxDistance}
```

**GitHub Links**
```
filePath → https://github.com/Olbrasoft/engineering-handbook/blob/main/{filePath}
```

## Troubleshooting

### API Connection Failed

**Error**: `Failed to fetch` or `Network error`

**Solutions**:
1. Check API is running: `curl http://localhost:5170/api/search?q=test`
2. Verify CORS headers in API response
3. Use HTTP server instead of opening file:// directly

### No Results

**Error**: `No results found`

**Solutions**:
1. Check database has documents: `psql -d handbook_search -c "SELECT COUNT(*) FROM documents;"`
2. Try broader search terms
3. Increase max distance threshold
4. Import documents: `dotnet run --project src/HandbookSearch.Cli -- import-all --path ~/GitHub/Olbrasoft/engineering-handbook`

### Incorrect Port

**Error**: API endpoint shows 404

**Solutions**:
1. Check API port in terminal output when starting API
2. Update `API_BASE_URL` in `index.html` line 122 if port differs from 5170

## Customization

### Change GitHub Repository

Edit line 123 in `index.html`:
```javascript
const GITHUB_REPO_URL = 'https://github.com/YOUR_ORG/YOUR_REPO/blob/main';
```

### Change API URL

Edit line 122 in `index.html`:
```javascript
const API_BASE_URL = 'http://localhost:YOUR_PORT';
```

### Styling

Tailwind CSS is loaded from CDN. To customize:
- Modify Tailwind classes in HTML
- Add custom CSS in `<style>` section
- Use Tailwind config for theme customization

## Data Storage

**Important**: This UI does NOT store any document content locally.

- Only displays links to source files in engineering-handbook
- All content remains in markdown files
- Database stores: embeddings, file paths, titles, content hashes
- Click links to view full documents on GitHub

## Browser Support

- Modern browsers with ES6+ support
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

## Production Deployment

For production, deploy to:
- **GitHub Pages**: Static hosting from repository
- **Netlify**: Drag-and-drop deployment
- **Vercel**: Zero-config deployment

Update CORS in API to allow production origin:
```csharp
policy.WithOrigins("https://your-domain.com")
```
