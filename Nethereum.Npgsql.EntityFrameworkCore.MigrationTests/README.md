# Migration tests (EF Core 8 ↔ EF Core 10)

Checks that EF Core migrations using `BigDecimal` keep working when an app moves between
EF Core 8 (`net8.0`) and EF Core 10 (`net10.0`), on PostgreSQL 15, 16, 17 and 18.

`Ef8Fixture/Migrations` holds the migrations and model snapshot that EF Core **8.0.4** generated
for `Demo.Api` (only the namespace was changed). They are compiled and run as-is under both runtimes.

These tests need Docker. The project name does not match the CI test glob (`**/*.Test.csproj`),
so CI builds it but does not run it.

## In-process tests (`Ef8MigrationsTests`)

```bash
dotnet test Nethereum.Npgsql.EntityFrameworkCore.MigrationTests
```

Runs on both frameworks, with Testcontainers starting one PostgreSQL container per major version:

- the EF8 snapshot matches the model (`HasPendingModelChanges()` is false, which EF Core 9+ requires before `Migrate()`)
- `Migrate()` from an empty database, column types, the `1.5` BigDecimal default, value round-trips
- `Down` migrations revert and re-apply
- the idempotent SQL script applies twice and matches `Migrate()`
- scaffolding with no model change gives an empty migration
- scaffolding a new BigDecimal column with a default generates `Nethereum.Util.BigDecimal.Parse("…")` in both the migration and the snapshot, and the resulting SQL applies on an EF8-migrated database (existing rows backfilled)

## Cross-runtime tests (`CrossRuntimeStages`)

```bash
./run-cross-runtime.sh          # PostgreSQL 15 16 17 18
./run-cross-runtime.sh 16       # one version
```

One database is shared across separate runs on different runtimes, in both orders:

| Order | Stage 1 | Stage 2 | Stage 3 |
|---|---|---|---|
| upgrade, then rollback | net8.0: apply `initial`, write rows | net10.0: apply the rest, verify, write rows | net8.0: no pending migrations or model changes, verify all rows, write |
| reverse | net10.0 | net8.0 | net10.0 |

Without `MIGRATION_CROSS_CS` (set by the script) these stages report Inconclusive.

## EF Core 10 note

On EF Core 10, scaffolding from a context that has already called `HasPendingModelChanges()`
diffs against an empty model (a full `CreateTable`). This happens without BigDecimal too.
`dotnet ef migrations add` always uses a fresh context, so the tests do the same.
