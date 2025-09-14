# Todos.Api

A minimal ASP.NET Core Web API using Repository + Unit of Work patterns with Entity Framework Core and PostgreSQL (Npgsql).

## Prerequisites

- .NET 9 SDK
- PostgreSQL running locally (default connection in appsettings)

## Configuration

- Update `Todos.Api/appsettings.json` → `ConnectionStrings:Default` as needed.
- For development, override in `appsettings.Development.json`.

## Database Migrations

Generate and apply EF Core migrations:

```zsh
cd Todos.Api
# Create initial migration
 dotnet ef migrations add InitialCreate
# Apply to the database
 dotnet ef database update
```

If `dotnet ef` is missing, install the tool globally:

```zsh
dotnet tool install --global dotnet-ef
```

## Run the API

```zsh
cd Todos.Api
dotnet run
```

Once running, explore endpoints:

- GET `https://localhost:5001/api/todos`
- GET `https://localhost:5001/api/todos/{id}`
- POST `https://localhost:5001/api/todos`
- PUT `https://localhost:5001/api/todos/{id}`
- DELETE `https://localhost:5001/api/todos/{id}`

Docs UIs:

- OpenAPI JSON: `/openapi/v1.json`
- Scalar UI: `/scalar`

Example POST body:

```json
{
  "title": "Buy milk",
  "description": "Skimmed"
}
```

## Notes

- `CreatedAtUtc` defaults at DB level and in code.
- `CompletedAtUtc` auto-updates when `IsCompleted` toggles.
- Repository uses `AsNoTracking` for reads.

## Observability

### Serilog + Seq

- Logging is configured via Serilog with Console and Seq sinks.
- Default Seq URL: `http://localhost:5341` (configurable in `appsettings*.json`).

Run Seq locally (Docker):

```zsh
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

Or with Homebrew (macOS):

```zsh
brew install --cask datalust/tap/seq
open -a Seq
```
