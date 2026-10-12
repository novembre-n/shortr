# Shortr

Shortr is a URL shortener API. You give it a long URL. It gives you a short code.
The short code redirects visitors to the long URL.

## What works now (v0.1.0)

- `POST /api/links` — create a short link. Returns `201` with a `Location` header.
- `GET /api/links/{code}` — redirect to the long URL. Returns `302`, or `404` for an unknown code.
- An invalid URL returns `400` with an error message.

## How to run it

You need the .NET 10 SDK.

```bash
git clone https://github.com/YOUR_USERNAME/shortr.git
cd shortr
dotnet run --project src/Shortr.Api
```

The API starts at `http://localhost:5213`.

The database is a single SQLite file (`shortr.db`). The API creates it on the
first start. No setup is needed.

Try it:

```bash
curl -X POST http://localhost:5213/api/links \
     -H "Content-Type: application/json" \
     -d '{"url": "https://example.com/your/long/url"}'
```

The response contains your short code. Then open
`http://localhost:5213/api/links/{code}` in a browser — it redirects.

## The architecture

Four projects. Dependencies point one way:

```
Shortr.Api ──► Shortr.Application ──► Shortr.Domain
     │                                      ▲
     └──► Shortr.Persistence ────────────────┘
```

- **Shortr.Domain** — the data model (`Link`). References nothing.
- **Shortr.Application** — the business logic (`LinkService`, `ShortCodeGenerator`) and the repository interface.
- **Shortr.Persistence** — the database code. Dapper + SQLite. All SQL lives here.
- **Shortr.Api** — the HTTP layer (controllers, DI wiring).

Why this shape: the domain cannot depend on anything. The business logic does
not know which database is in use. You can replace SQLite with another database
by changing one registration in `Program.cs`.

## Design decisions

**Dapper, not EF Core.** This project uses plain SQL. A URL shortener is a
key-value lookup at heart. SQL makes that simple and fast to read.

**Short codes are random.** The code is 7 random base62 characters. 62 to the
power of 7 is about 3.5 trillion codes. When two requests generate the same code
by chance, the database rejects the second one. The service then generates a new
code. The database is the source of truth for code uniqueness.

**Schema applies at startup.** The schema lives in `schema.sql` and runs on
every start. The script uses `IF NOT EXISTS`, so it is safe to run again. No
migration tool is needed at this size.

**302, not 301.** A permanent (301) redirect is cached by browsers. The browser
then skips this API on future clicks. The API would lose the ability to track
clicks and to expire links. So the redirect is temporary (302).

**Validation lives in the service.** The model keeps plain properties for
Dapper to map. `LinkService` checks the URL before anything touches the database.