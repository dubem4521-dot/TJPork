# T&JPork

Artisanal heritage pork and smoked meats, sold online. Real shop, running on a home server.

**Live site:** https://tjpork.tail0e09f1.ts.net

---

## What This Is

A small ecommerce site for a real business, premium, pasture-raised artisanal pork products. It allows customers to browse cuts of pork, add items to their cart, select a delivery date, and place orders. It also includes an admin dashboard for managing inventory, tracking orders, and updating store information.

---

## Stack

| Layer | Technology | Where it runs |
|:------|:-----------|:--------------|
| Web framework | ASP.NET Core (.NET 10) | Docker container |
| Database | PostgreSQL via Supabase | Cloud, managed |
| Image storage | Supabase Storage | Cloud, managed |
| Caching | Redis (dev only) | Docker container |
| Email (dev) | MailHog | Docker container |
| Hosting | HP t520 thin client | Home, behind Tailscale Funnel |
| CI/CD | GitHub Actions | Builds image, pushes to Docker Hub |
| Reverse proxy | Tailscale Funnel | Provides the public HTTPS URL |

---

## Repo Layout

```
TJPork/
    .github/
        workflows/
            build-and-push.yml
            deploy.yml
    src/
        TJPork.Core/
        TJPork.Infrastructure/
        TJPork.Web/
    tests/
        TJPork.Tests/
    .dockerignore
    .editorconfig
    .env.example
    .gitignore
    docker-compose.yml
    docker-compose.prod.yml
    Dockerfile
    README.md
    TJPork.sln
```

---

## How To Run It Locally

You need Docker Desktop (or Docker Engine on Linux) and a Supabase account.

### 1. Clone and configure

```bash
git clone https://github.com/dubem4521-dot/TJPork.git
cd TJPork
cp .env.example .env
```

Edit `.env` and fill in your Supabase credentials:

```
DATABASE_URL=Host=aws-0-eu-central-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project>;Password=<your-password>
SUPABASE_URL=https://<your-project>.supabase.co
SUPABASE_KEY=<your-service-role-key>
```

**Note:** use the connection pooler URL, not the direct URL. The direct one is IPv6 only and fails inside Docker.

### 2. Run with Docker Compose

```bash
docker compose up -d
```

Open `http://localhost:5000` in your browser. The first time the app starts, it creates the database schema and seeds default products and an admin user.

MailHog runs on `http://localhost:8025` for catching outgoing emails in development.

### 3. Run without Docker (optional)

If you have the .NET 10 SDK installed:

```bash
dotnet restore
dotnet run --project src/TJPork.Web
```

The app falls back to a local SQLite database if `DATABASE_URL` is not set.

---

## How It Deploys

Every push to `main` triggers a GitHub Actions workflow that:

1. Builds the Docker image.
2. Tags it with both `latest` and the commit SHA.
3. Pushes it to Docker Hub.
4. Connects to the home server via Tailscale.
5. SSHes in and pulls the new image.
6. Restarts the container with `docker compose up -d`.

The server runs the ASP.NET Core app behind Tailscale Funnel, which provides a public HTTPS URL without opening any router ports.

---

## Environment Variables

| Variable | Purpose | Required |
|:---------|:--------|:---------|
| `DATABASE_URL` | PostgreSQL connection string (Supabase pooler) | Yes (prod) |
| `SUPABASE_URL` | Supabase project URL | Yes (for images) |
| `SUPABASE_KEY` | Supabase `service_role` key, for server-side uploads | Yes (for images) |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` | Yes |
| `ASPNETCORE_URLS` | URL and port the app listens on | Yes |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Trust proxy headers (needed behind Funnel) | Yes (prod) |
| `ConnectionStrings__Redis` | Redis connection string | No (dev only) |
| `EmailSettings__*` | SMTP settings for order emails | No (currently unused) |

See `.env.example` for the full list.

---

## Testing

```bash
dotnet test
```

24 tests, covering core business logic and service layers.

**Currently not run in CI.** Adding a `dotnet test` job before the build is on the roadmap.

---



## License

Personal project. No license applied. Do not distribute.

---

## Author

Built and maintained by Max. Contact via GitHub.
