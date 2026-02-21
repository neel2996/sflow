# SourceFlow

Chrome extension + .NET backend for recruiters to scan LinkedIn profiles against job descriptions using AI.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/sql-server) (or LocalDB / Docker)
- [Gemini API key](https://aistudio.google.com/apikey)
- Google Chrome

## Project Structure

```
SourceFlow/
├── backend/
│   └── SourceFlow.Api/
│       ├── Controllers/        # Auth, User, Jobs, Analysis
│       ├── Data/               # EF Core DbContext
│       ├── Dtos/               # Request/response models
│       ├── Models/             # Entity models
│       ├── Services/           # AI, Credit, Cache services
│       ├── Program.cs          # App entry point
│       └── appsettings.json    # Configuration
└── extension/
    ├── src/
    │   ├── api.js              # API client
    │   ├── content.jsx         # Content script entry
    │   ├── panel.jsx           # LinkedIn side panel (React)
    │   └── popup.jsx           # Extension popup (React)
    ├── manifest.json           # Chrome Manifest V3
    ├── background.js           # Service worker
    ├── popup.html              # Popup HTML shell
    ├── build.mjs               # esbuild config
    └── package.json
```

## Backend Setup

### 1. Configure appsettings.json

Edit `backend/SourceFlow.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SourceFlow;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long-here",
    "Issuer": "SourceFlow",
    "Audience": "SourceFlow"
  },
  "Gemini": {
    "ApiKey": "your-actual-gemini-api-key",
    "Model": "gemini-2.0-flash"
  }
}
```

> For Docker SQL Server:
> ```
> docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStr0ng!Pass" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
> ```
> Connection string: `Server=localhost;Database=SourceFlow;User Id=sa;Password=YourStr0ng!Pass;TrustServerCertificate=True;`

### 2. Restore packages and create database

```bash
cd backend/SourceFlow.Api

# Restore NuGet packages
dotnet restore

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to create database
dotnet ef database update
```

### 3. Run the backend

```bash
cd backend/SourceFlow.Api
dotnet run
```

The API starts at `http://localhost:5000` by default.

## Extension Setup

### 1. Install dependencies and build

```bash
cd extension
npm install
npm run build
```

This bundles `src/content.jsx` and `src/popup.jsx` into `dist/`.

### 2. Load in Chrome

1. Open Chrome → `chrome://extensions/`
2. Enable **Developer mode** (top-right toggle)
3. Click **Load unpacked**
4. Select the `extension/` folder

### 3. Development mode

For auto-rebuilding on file changes:

```bash
cd extension
npm run watch
```

After changes, click the refresh icon on the extension card in `chrome://extensions/`.

## Usage

1. **Register/Login** — Click the SourceFlow extension icon → register or log in
2. **Add a Job** — In the popup, paste a job title and description → click "Add Job"
3. **Scan a Profile** — Navigate to any LinkedIn profile (`linkedin.com/in/...`)
   - The SourceFlow panel appears on the right side
   - Select a job from the dropdown
   - Click **Scan Profile**
4. **View Results** — Match score, strengths, missing skills, summary, and outreach message appear in the panel

## API Endpoints

| Method | Path              | Auth     | Description              |
|--------|-------------------|----------|--------------------------|
| POST   | `/auth/register`  | No       | Create account (10 free credits) |
| POST   | `/auth/login`     | No       | Get JWT token            |
| GET    | `/user/me`        | JWT      | Current user info        |
| POST   | `/jobs`           | JWT      | Create job description   |
| GET    | `/jobs`           | JWT      | List user's jobs         |
| POST   | `/analysis/scan`  | JWT      | Scan profile against job |

### Scan Request Body

```json
{
  "job_id": 1,
  "profile_url": "https://www.linkedin.com/in/example",
  "profile_text": "Name: John Doe\nHeadline: Senior Engineer..."
}
```

### Scan Response

```json
{
  "match_score": 76,
  "strengths": [".NET Core", "Azure", "SQL Server"],
  "missing_skills": ["Kubernetes", "Terraform"],
  "summary": "Senior backend dev with strong cloud exposure.",
  "outreach_message": "Hi John, I saw your .NET + Azure experience..."
}
```

## Database Schema

- **Users** — Id, Email, PasswordHash, Credits, CreatedAt
- **Jobs** — Id, UserId, Title, Description, CreatedAt
- **CreditTransactions** — Id, UserId, CreditsChanged, Type, CreatedAt
- **ProfileAnalysisCache** — Id, ProfileUrl, JobId, JsonResult, MatchScore, CreatedAt
  - Unique index on (ProfileUrl, JobId)

## Notes

- New accounts get **10 free credits**
- Each scan costs **1 credit** (cached results are free)
- The extension only activates on `linkedin.com/in/*` profile pages
- Profile text is extracted from visible page content (name, headline, about, experience, education, skills)
- Backend uses `gemini-2.0-flash` by default — change `Gemini:Model` in appsettings to use a different model (e.g. `gemini-2.5-pro`)
