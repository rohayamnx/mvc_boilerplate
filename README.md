# mvc_boilerplate

An ASP.NET Core MVC boilerplate for internal web applications, featuring cookie-based authentication, Active Directory integration, StaffHub API connectivity, role-based access control (RBAC), and a Tailwind CSS + Alpine.js frontend.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10) |
| Database | SQL Server + Entity Framework Core 10 |
| Authentication | Cookie auth, Active Directory (LDAP), Azure AD (via StaffHub) |
| Frontend CSS | Tailwind CSS v3 + esbuild |
| Frontend JS | Alpine.js, ApexCharts, Chart.js, FullCalendar, Flatpickr, Font Awesome |
| External API | StaffHub API (HTTP client factory + in-memory cache) |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (local or remote)
- [Node.js](https://nodejs.org/) (for building CSS/JS assets)

---

## Getting Started

### 1. Clone the repository

```powershell
git clone https://github.com/rohayamnx/mvc_boilerplate.git
cd mvc_boilerplate
```

### 2. Configure app settings

The app runs on `appsettings.Development.json` (already in `.gitignore`). A template file `appsettings.Development.json.template` is provided — copy it and fill in your values:

```powershell
Copy-Item appsettings.Development.json.template appsettings.Development.json
```

Then edit `appsettings.Development.json` and set at minimum the database connection string:

```json
{
  "ProjectName": "mvc_boilerplate",
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YOUR_SERVER>;Database=mvc_boilerplate;Trusted_Connection=True;TrustServerCertificate=True;Integrated Security=True;MultipleActiveResultSets=true"
  },
  "App": {
    "BaseUrl": "https://localhost:8101"
  },
  "ActiveDirectory": {
    "LdapPath": "<YOUR_LDAP_PATH>",
    "Domain": "<YOUR_DOMAIN>",
    "Username": "<YOUR_AD_USERNAME>",
    "Password": "<YOUR_AD_PASSWORD>"
  },
  "StaffHubApi": {
    "BaseUrl": "https://localhost:7101",
    "ApiKey": "<YOUR_API_KEY>"
  },
  "CacheSettings": {
    "StaffHubApi": {
      "DurationMinutes": 2
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

> Active Directory and StaffHub API are optional. The app runs without them; AD login and Azure AD buttons are silently disabled when unavailable.

### 3. Install frontend dependencies

```powershell
npm install
```

### 4. Build CSS and JS assets

**Development (watch mode):**
```powershell
npm run build:css       # Tailwind CSS in watch mode
```

**One-time build:**
```powershell
npm run build           # CSS (unminified) + JS bundle
```

**Production build (minified):**
```powershell
npm run build:prod
```

**Run app + CSS watcher together:**
```powershell
npm run watch
```

### 5. Apply database migrations

```powershell
dotnet ef database update
```

This runs all EF Core migrations and seeds the default SuperAdmin account.

### 6. Run the application

```powershell
dotnet watch run
```

The app starts at `https://localhost:8101` (or whatever port is configured).

---

## Default Credentials

| Field | Value |
|---|---|
| User ID | `ADM001` |
| Password | `1234Abcd#` |
| Role | SuperAdmin |

> **Change this password immediately after first login.**

---

## Project Structure

```
mvc_boilerplate/
├── Controllers/            # MVC controllers
│   ├── AccountController       # Login, logout, Azure AD sign-in
│   ├── DashboardController     # Main dashboard
│   ├── UserAccountController   # User management (CRUD)
│   ├── RolePermissionController# Role & permission management
│   ├── ExamplesController      # UI component examples
│   └── HomeController          # Home / landing
├── Data/
│   ├── ApplicationDbContext    # EF Core DbContext
│   └── SeedData                # Default roles and admin user
├── Extensions/
│   └── PaginationExtensions    # IQueryable pagination helpers
├── Migrations/             # EF Core migration files
├── Models/
│   ├── Dto/                # Data transfer objects
│   ├── Session/            # Session state models
│   ├── UI/                 # UI configuration models
│   ├── ViewModels/         # View-specific models
│   ├── Module.cs           # Permission module
│   ├── Permission.cs       # Permission entry
│   ├── Role.cs             # User role
│   ├── RolePermission.cs   # Role ↔ Permission join
│   └── UserAccount.cs      # Application user
├── Services/
│   ├── ADAuthenticationService     # Active Directory LDAP auth (Windows only)
│   ├── StaffHubApiService          # StaffHub REST API client
│   └── StaffHubAuthInfoService     # StaffHub auth info (Azure AD URL)
├── ViewComponents/
│   ├── DeleteModalViewComponent    # Reusable delete confirmation modal
│   ├── NotificationViewComponent   # Toast / alert notifications
│   └── PaginationViewComponent     # Page navigation component
├── Views/                  # Razor views (.cshtml)
├── wwwroot/                # Static assets (CSS, JS, images)
├── appsettings.json        # Base configuration
├── appsettings.Development.json  # Local dev config (git-ignored)
├── tailwind.config.js      # Tailwind CSS configuration
├── package.json            # Node dependencies and build scripts
└── Program.cs              # Application entry point
```

---

## Authentication Flow

1. **Local DB** — Password validated against a bcrypt hash stored in `UserAccounts`.
2. **Active Directory** — LDAP credentials validated against the configured domain (Windows only; falls back gracefully).
3. **Azure AD (via StaffHub)** — If the StaffHub API returns an Azure redirect URL, an "Azure AD" login button appears on the login page.

The login strategy tries AD first, then falls back to the local database.

---

## Role-Based Access Control

Modules and permissions are stored in the database. Each role is assigned a set of permissions, and users are assigned a single role. Permission checks can be applied per action via the `RolePermission` model.

---

## NPM Scripts Reference

| Script | Description |
|---|---|
| `npm run build` | Build CSS (once) + JS bundle |
| `npm run build:prod` | Minified CSS + JS bundle (production) |
| `npm run build:css` | Tailwind CSS in watch mode |
| `npm run build:css:once` | Build Tailwind CSS once |
| `npm run build:js` | Bundle JS with esbuild |
| `npm run dev` | Alias for `build:css` (watch) |
| `npm run watch` | CSS watch + `dotnet watch run` concurrently |

---

## EF Core Migrations

```powershell
# Add a new migration
dotnet ef migrations add MigrationName

# Apply pending migrations
dotnet ef database update

# Revert the last migration
dotnet ef database update PreviousMigrationName
```

---

## Git Setup

See [GIT-SETUP.md](./GIT-SETUP.md) for a step-by-step guide on initializing and pushing this project to GitHub.
