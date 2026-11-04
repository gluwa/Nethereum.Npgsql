using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nethereum.Npgsql.EntityFrameworkCore.MigrationTests.Ef8Fixture;
using Nethereum.Util;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Nethereum.Npgsql.EntityFrameworkCore.MigrationTests;

/// <summary>PostgreSQL containers (one per major version, started lazily) and context helpers.</summary>
public static class TestDb
{
    /// <summary>The PostgreSQL major versions we run on Azure Flexible Server.</summary>
    public static IEnumerable<object[]> PostgresVersions =>
        new[] { "15", "16", "17", "18" }.Select(v => new object[] { v });

    public static string EfVersion => typeof(DbContext).Assembly.GetName().Version!.ToString(3);
    public static string Runtime => RuntimeInformation.FrameworkDescription;

    static readonly ConcurrentDictionary<string, Lazy<Task<PostgreSqlContainer>>> Containers = new();

    /// <summary>Creates a fresh, empty database on the given PostgreSQL major version.</summary>
    public static async Task<string> CreateDatabaseAsync(string postgresVersion)
    {
        var container = await Containers.GetOrAdd(postgresVersion, v => new(async () =>
        {
            var c = new PostgreSqlBuilder($"postgres:{v}").Build();
            await c.StartAsync();
            return c;
        })).Value;

        var name = "t_" + Guid.NewGuid().ToString("N")[..12];
        await using (var conn = new NpgsqlConnection(container.GetConnectionString()))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE {name}", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString;
    }

    public static async Task DisposeAllAsync()
    {
        foreach (var c in Containers.Values.Where(l => l.IsValueCreated))
            await (await c.Value).DisposeAsync();
    }

    public static NumberContext Context(string connectionString, Action<ModelBuilder>? extraModel = null)
    {
        var options = new DbContextOptionsBuilder<NumberContext>()
            .UseNpgsql(connectionString, o => o.UseBigDecimal());
        if (extraModel is not null)
            options.ReplaceService<IModelCacheKeyFactory, ExtraModelCacheKeyFactory>();
        return new NumberContext(options.Options) { ExtraModel = extraModel };
    }

    public static async Task<string?> ScalarAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        return (await cmd.ExecuteScalarAsync())?.ToString();
    }

    /// <summary>Values that exercise the converter: zero, signs, tiny fractions, 2^256-1.</summary>
    public static readonly BigDecimal[] SampleValues = new[]
    {
        "0", "1", "-1", "1.5", "0.000000000000000001", "-98765.4321",
        "123456789012345678901234567890.123456789",
        "115792089237316195423570985008687907853269984665640564039457584007913129639935",
        "0.000000000000000000000000000001",
    }.Select(BigDecimal.Parse).ToArray();
}

[TestClass]
public static class AssemblyHooks
{
    [AssemblyCleanup]
    public static async Task Cleanup() => await TestDb.DisposeAllAsync();
}
