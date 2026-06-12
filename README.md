# TherExNow / HepNow Backend

## Project Overview

This is the backend API for **HepNow / TherExNow**, a home exercise program
(HEP) application. It is an ASP.NET Core 6 application that exposes:

- A **GraphQL API** (HotChocolate) for the main application data — exercises,
  routines, categories, products, plans, memberships, requests, notifications,
  shopping cart, etc.
- A small set of **REST controllers** for account/login, file uploads, and
  payment/subscription webhooks (Stripe, Apple App Store Server Notifications,
  Google Play real-time developer notifications).
- **Firebase Authentication** as the identity provider (JWT bearer tokens
  validated against `https://securetoken.google.com/{ProjectName}`).
- **EF Core 6** with the **Pomelo MySQL** provider for persistence, including
  automatic migrations on startup.

Solution layout:

```
Aps.Apps.HepNow.sln
├── Aps.Apps.HepNow.Api/      # ASP.NET Core Web API (GraphQL + REST)
└── Aps.Apps.HepNow.Test/     # xUnit service-layer tests (EF Core InMemory provider)
```

---

## Prerequisites

| Requirement | Version / Notes |
|---|---|
| **.NET SDK** | **.NET 6.0** (project targets `net6.0`). The **ASP.NET Core 6.0 runtime** is required, not just the .NET 6 runtime — `Microsoft.NET.Sdk.Web` apps need it to run. |
| **MySQL** | 8.x (or any MySQL-compatible server). The app calls `Database.Migrate()` on startup, so the database itself does not need to exist beforehand — only the server/credentials do. |
| **Firebase project** | A Firebase project with Authentication enabled, plus a **service account JSON key** for the Admin SDK. |
| **Private NuGet feed** | The project depends on private packages `Aps.CommonBack.Auth`, `Aps.CommonBack.Base`, and `Apsy.Common.Api.Auth`. These are **not** on nuget.org — you need access to the team's private package source (see [azure-pipelines.yml](azure-pipelines.yml), which restores from an Azure DevOps Artifacts feed). Add that feed to your NuGet config, or place the `.nupkg`/built DLLs in a local folder and register it as a package source. |
| **EF Core tools** (optional, for managing migrations) | `dotnet tool install --global dotnet-ef --version 6.*` |

### Installing the .NET 6 SDK + ASP.NET Core runtime

- **macOS (Homebrew):** `brew install dotnet@6` — this installs both the SDK
  and the matching ASP.NET Core runtime under one formula.
- **Linux/Windows:** download the **.NET 6.0 SDK** (which bundles the
  ASP.NET Core runtime) from
  https://dotnet.microsoft.com/download/dotnet/6.0.

> If you have multiple SDKs installed (e.g. .NET 6 and .NET 10), `dotnet` will
> resolve to whichever is first on `PATH`. Run `dotnet --list-sdks` and
> `dotnet --list-runtimes` to confirm both `Microsoft.NETCore.App 6.0.x` **and**
> `Microsoft.AspNetCore.App 6.0.x` are present — the build will succeed with
> only the SDK, but `dotnet run` / `dotnet test` will fail at startup with a
> "You must install or update .NET to run this application" error if the
> ASP.NET Core 6.0 **runtime** is missing.

---

## Setup Instructions

### 1. Clone and restore

```bash
git clone <repo-url>
cd TherExNow-Backend
dotnet restore
```

> Restore will fail with `NU1101`/`NU1102` errors for `Aps.CommonBack.*` and
> `Apsy.Common.Api.Auth` if the private NuGet feed (see Prerequisites) is not
> configured.

### 2. Set up MySQL

Start a local MySQL 8.x server and create a user/database the app can use
(the schema itself is created automatically via migrations on first run).

```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS hepnow;"
```

### 3. Get Firebase service account credentials

The Firebase service account JSON files live under
`Aps.Apps.HepNow.Api/Data/` (`firebase-dev.json`, `firebase-qa.json`,
`firebase-stg.json`, `firebase-prd.json`). Obtain the correct file for your
target environment from your Firebase project settings
(**Project Settings → Service Accounts → Generate new private key**) and
place it in `Aps.Apps.HepNow.Api/Data/`.

### 4. Configure environment variables

Copy [.env.example](.env.example) to `.env` and fill in real values:

```bash
cp .env.example .env
```

> **.NET does not load `.env` files automatically.** Export the variables into
> your shell session before running the app, e.g.:
>
> ```bash
> export $(grep -v '^#' .env | xargs)
> ```
>
> Alternatively, set the values via `dotnet user-secrets` (per-developer,
> never committed) or your process manager / container runtime's environment
> configuration.

ASP.NET Core automatically overrides `appsettings.json` values with matching
environment variables, using `__` (double underscore) for nested keys — e.g.
the environment variable `ConnectionStrings__DbConnection` overrides
`ConnectionStrings:DbConnection` in `appsettings.json`. See `.env.example` for
the full list of variables and what each one configures.

### 5. Restore, build

```bash
dotnet restore
dotnet build
```

---

## Running the Project

### Run the API locally

```bash
cd Aps.Apps.HepNow.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

By default (see `Properties/launchSettings.json`) the API listens on:

- `https://localhost:7079`
- `http://localhost:5079`

Once running:

- **GraphQL (Altair UI):** `/ui/altair`
- **Swagger UI:** `/swagger`
- **GraphQL endpoint:** `/graphql`

On startup, the app calls `context.Database.Migrate()` against
`ConnectionStrings:DbConnection`, so the configured MySQL database is created
and migrated to the latest schema automatically — **no manual `dotnet ef
database update` is required for normal local runs.**

### Running tests

```bash
dotnet test
```

The test project (`Aps.Apps.HepNow.Test`) uses EF Core's **InMemory**
provider, so it does **not** require a MySQL connection.

### Managing EF Core migrations (when changing entity models)

```bash
cd Aps.Apps.HepNow.Api

# Add a new migration after changing entities in Models/Entities
dotnet ef migrations add <MigrationName>

# Apply migrations manually (optional — the app does this automatically on startup)
dotnet ef database update
```

---

## Build & Deployment

### Building for production

```bash
dotnet publish Aps.Apps.HepNow.Api/Aps.Apps.HepNow.csproj -c Release -o ./publish
```

This produces a framework-dependent deployment in `./publish`. Run it with:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet ./publish/Aps.Apps.HepNow.dll
```

### Deployment notes (Linux hosting)

- The target server needs the **ASP.NET Core 6.0 runtime** installed (or
  publish a **self-contained** build with `-p:SelfContained=true -r
  linux-x64` to bundle the runtime).
- Set `ASPNETCORE_ENVIRONMENT=Production` and supply all variables from
  `.env.example` via your hosting platform's environment/secrets mechanism
  (systemd `EnvironmentFile=`, Docker `env_file`, Kubernetes `Secret`, etc.) —
  **do not** rely on values committed to `appsettings.json`.
- Put the app behind a reverse proxy (nginx, etc.) that forwards to Kestrel,
  per standard ASP.NET Core Linux deployment guidance.
- `app.UseWebSockets()` is enabled for GraphQL subscriptions — make sure your
  reverse proxy is configured to allow WebSocket upgrades on the `/graphql`
  path.
- The existing CI pipelines (`azure-pipelines.yml`, `azure-pipelines-1.yml`)
  target a **Windows** build agent and use `MSBuild`/`VSBuild` tasks oriented
  at IIS/Azure App Service deployment. If moving to a Linux host, replace
  these with a `dotnet publish` + container or systemd-based pipeline.

### Database migrations in production

Since `DataInitializer.Initialize` runs `context.Database.Migrate()` on every
app startup, deploying a new version with new migrations will apply them
automatically. For zero-downtime deployments with multiple instances, consider
running migrations as a separate release step instead, to avoid concurrent
instances racing to apply the same migration.

---

## Configuration Reference

All configuration keys are defined in
[Aps.Apps.HepNow.Api/appsettings.json](Aps.Apps.HepNow.Api/appsettings.json)
and documented with environment-variable equivalents in
[.env.example](.env.example). At a glance:

| Section | Used for |
|---|---|
| `ConnectionStrings:DbConnection` | MySQL connection string (Pomelo EF Core provider) |
| `ProjectName`, `ProjectJsonUri`, `ApiKey` | Firebase Authentication (JWT issuer/audience, Admin SDK service account file, Web API key for email/password login) |
| `env` | Logical environment name (`dev`/`qa`/`stg`/`prd`) — `prd` disables verbose GraphQL error details |
| `TermsAndConditionUrl` | Linked from confirmation emails |
| `SendGridConfig` | SendGrid transactional email |
| `TwilioConfig` | SMS notifications |
| `StripeSettings` | Stripe billing + webhook signature verification (`/api/StripeWebhook`) |
| `AppleSharedSecret` | Apple in-app purchase receipt verification |
| `TranslatorConfig` | AWS Translate |
| `S3Credentials` | AWS S3 file uploads (`/api/Upload`) |
| `SES-Credentials` | AWS SES account/registration emails |

---