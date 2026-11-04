using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nethereum.Npgsql.EntityFrameworkCore.MigrationTests.Ef8Fixture;
using Nethereum.Util;

namespace Nethereum.Npgsql.EntityFrameworkCore.MigrationTests;

/// <summary>
/// One database shared across separate test runs on different runtimes, so a database migrated and
/// written by EF Core 8 is then used by EF Core 10 and back (upgrade, then rollback), or the reverse.
/// Driven by run-cross-runtime.sh, which sets MIGRATION_CROSS_CS and runs one stage per framework.
/// Without MIGRATION_CROSS_CS the stages are skipped (Inconclusive).
/// </summary>
[TestClass]
public class CrossRuntimeStages
{
    const string Initial = "20240418184813_initial";
    static readonly BigDecimal DbDefault = BigDecimal.Parse("1.5");

    static string ConnectionString =>
        Environment.GetEnvironmentVariable("MIGRATION_CROSS_CS") is { Length: > 0 } cs
            ? cs
            : throw new AssertInconclusiveException("MIGRATION_CROSS_CS not set; run run-cross-runtime.sh");

    /// <summary>Stage 1: apply only the first EF8 migration and write rows.</summary>
    [TestMethod]
    public async Task Stage1_seed_on_initial_migration()
    {
        await using var ctx = TestDb.Context(ConnectionString);
        await ctx.GetService<IMigrator>().MigrateAsync(Initial); // DatabaseFacade overload is EF9+
        Assert.AreEqual(0, await ctx.Numbers.CountAsync(), "stage 1 expects an empty database");

        // The current model already maps larger_number_optional, which the initial schema lacks,
        // so write through SQL (still BigDecimal parameters via this library) instead of SaveChanges.
        foreach (var v in TestDb.SampleValues)
            await ctx.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO numbers (large_number) VALUES ({v})");

        Console.WriteLine($"Stage1 on EF {TestDb.EfVersion} / {TestDb.Runtime}: seeded {TestDb.SampleValues.Length} rows");
    }

    /// <summary>Stage 2: other runtime applies the remaining migration and adds rows.</summary>
    [TestMethod]
    public async Task Stage2_upgrade_to_latest_and_write()
    {
        await using (var ctx = TestDb.Context(ConnectionString))
        {
            CollectionAssert.AreEqual(new[] { Initial }, (await ctx.Database.GetAppliedMigrationsAsync()).ToArray());
            await ctx.Database.MigrateAsync();
            Assert.IsFalse((await ctx.Database.GetPendingMigrationsAsync()).Any());

            var seeded = await ctx.Numbers.OrderBy(n => n.Id).ToListAsync();
            CollectionAssert.AreEqual(TestDb.SampleValues, seeded.Select(n => n.LargeNumber).ToArray(), "stage 1 rows");
            Assert.IsTrue(seeded.All(n => n.SecondLargeNumber == DbDefault), "new column backfilled with the BigDecimal default");

            foreach (var v in TestDb.SampleValues)
                ctx.Numbers.Add(new Number { LargeNumber = v, SecondLargeNumber = v });
            await ctx.SaveChangesAsync();
        }
        Console.WriteLine($"Stage2 on EF {TestDb.EfVersion} / {TestDb.Runtime}: migrated to latest, added {TestDb.SampleValues.Length} rows");
    }

    /// <summary>Stage 3: first runtime again (e.g. rollback of the app); schema and all rows must be usable.</summary>
    [TestMethod]
    public async Task Stage3_switch_back_and_verify()
    {
        await using (var ctx = TestDb.Context(ConnectionString))
        {
            Assert.IsFalse((await ctx.Database.GetPendingMigrationsAsync()).Any(), "history written by the other EF version is recognised");
            Assert.IsFalse(ctx.Database.HasPendingModelChanges());
            await ctx.Database.MigrateAsync(); // must be a no-op

            var rows = await ctx.Numbers.OrderBy(n => n.Id).ToListAsync();
            var n = TestDb.SampleValues.Length;
            Assert.AreEqual(2 * n, rows.Count);
            for (var i = 0; i < n; i++)
            {
                Assert.AreEqual(TestDb.SampleValues[i], rows[i].LargeNumber, $"stage 1 row {i}");
                Assert.AreEqual(DbDefault, rows[i].SecondLargeNumber, $"stage 1 row {i} default");
                Assert.AreEqual(TestDb.SampleValues[i], rows[n + i].LargeNumber, $"stage 2 row {i}");
                Assert.AreEqual(TestDb.SampleValues[i], rows[n + i].SecondLargeNumber, $"stage 2 row {i} optional");
            }

            var big = TestDb.SampleValues[7];
            Assert.AreEqual(2, await ctx.Numbers.CountAsync(x => x.LargeNumber == big), "server-side comparison on rows from both runtimes");

            ctx.Numbers.Add(new Number { LargeNumber = big });
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = TestDb.Context(ConnectionString))
        {
            var last = await ctx.Numbers.OrderByDescending(x => x.Id).FirstAsync();
            Assert.AreEqual(TestDb.SampleValues[7], last.LargeNumber);
            Assert.AreEqual(DbDefault, last.SecondLargeNumber);
        }
        Console.WriteLine($"Stage3 on EF {TestDb.EfVersion} / {TestDb.Runtime}: all rows verified");
    }
}
