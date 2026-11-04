# ICMarkets Blockchain API

.NET 10 Minimal API that stores BlockCypher data in SQLite.

## Run

With .NET 10 SDK:

```bash
dotnet run --project ICMarkets.Api
```

With Docker:

```bash
docker compose up --build
```

Swagger: `http://localhost:8080/swagger` (Docker) or the URL printed by `dotnet run`. Health: `/health`.

## Tests

Requires the .NET 10 SDK. Run all tests:

```bash
dotnet test ICMarkets.sln
```

Run one project:

```bash
dotnet test ICMarkets.UnitTests/ICMarkets.UnitTests.csproj
dotnet test ICMarkets.IntegrationTests/ICMarkets.IntegrationTests.csproj
```

`ICMarkets.UnitTests` cover application behavior; `ICMarkets.IntegrationTests` use an in-memory SQLite database.

## API

- `POST /api/blockchains/collect` — collect all networks; successful results are saved even if another network fails. Response includes a result for each network.
- `POST /api/blockchains/{network}/collect` — collect one network.
- `GET /api/blockchains/{network}/history?from=2026-11-01T00:00:00Z&to=2026-11-04T00:00:00Z&limit=100` — history in `[from, to)`, newest first. Both dates are required; `limit` is 1–500.
- `GET /health` — database health.

Networks: `EthereumMainnet`, `DashMainnet`, `BitcoinMainnet`, `BitcoinTestnet3`, `LitecoinMainnet`.

## How it works

The API fetches BlockCypher JSON and stores it with its network in SQLite. A background worker collects all networks at startup and every five minutes by default. Collection interval and startup behavior are configurable. Upstream calls use retries, timeouts, and a circuit breaker. Logs go to console and a rolling file.

## Configuration

Edit `ICMarkets.Api/appsettings.json`:

- `ConnectionStrings:Blockchain` — SQLite database location.
- `BlockCypher:BaseUrl`, `BlockCypher:Networks` — upstream URL and network paths.
- `Collection:Enabled`, `CollectOnStartup`, `Interval` — background collection schedule.
- `HttpResilience` — retries, timeouts, and circuit breaker.
- `Serilog` — log levels, console/file sinks, file path and retention.
- `Cors:AllowedOrigins` — allowed browser origins.

## Scaling limits

This setup is intended for one API instance. SQLite and Docker volumes are local to the deployment, and each API instance starts its own collection worker; replicas can create duplicate snapshots and contend for writes. Before scaling out, move data to a shared database such as PostgreSQL and run collection in one dedicated worker (or coordinate workers with a distributed lock). Use centralized logging if logs must be available across instances.
