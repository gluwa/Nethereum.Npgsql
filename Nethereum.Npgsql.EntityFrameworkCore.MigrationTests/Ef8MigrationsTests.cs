using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Npgsql.EntityFrameworkCore.MigrationTests.Ef8Fixture;
using Nethereum.Util;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Design.Internal;

namespace Nethereum.Npgsql.EntityFrameworkCore.MigrationTests;

/// <summary>
/// Runs the EF Core 8 generated migrations (Ef8Fixture/Migrations) under the EF Core version this
/// assembly targets: EF Core 8 on net8.0, EF Core 10 on net10.0.
/// </summary>
[TestClass]
public class Ef8MigrationsTests
{
    const string Initial = "20240418184813_initial";
    const string DefaultValue = "20240508193043_DefaultValueBigDecimal";

    static readonly BigDecimal FeeDefault = BigDecimal.Parse("0.000000000000000001");

    /// <summary>A model change on top of the EF8 snapshot: a new BigDecimal column with a default.</summary>
    static void AddFeeColumn(ModelBuilder b) =>
        b.Entity<Number>().Property<BigDecimal>("Fee").HasColumnName("fee").HasDefaultValue(FeeDefault);

    [TestMethod]
    public void Ef8_snapshot_matches_model_with_no_pending_changes()
    {
        using var ctx = TestDb.Context("Host=localhost;Database=unused");

        var snapshot = ctx.GetService<IMigrationsAssembly>().ModelSnapshot!;
        Assert.AreEqual("8.0.4", snapshot.Model.GetProductVersion(), "fixture should stay EF8-generated");
        CollectionAssert.AreEqual(new[] { Initial, DefaultValue }, ctx.Database.GetMigrations().ToArray());

        // EF Core 9+ also refuses to Migrate() when this is true (PendingModelChangesWarning).
        Assert.IsFalse(ctx.Database.HasPendingModelChanges(), $"EF {TestDb.EfVersion} sees model changes against the EF8 snapshot");
    }

    [TestMethod]
    [DynamicData(nameof(TestDb.PostgresVersions), typeof(TestDb))]
    public async Task Migrate_from_empty_database_applies_ef8_migrations(string pg)
    {
        var cs = await TestDb.CreateDatabaseAsync(pg);
        await using (var ctx = TestDb.Context(cs))
        {
            await ctx.Database.MigrateAsync();
            CollectionAssert.AreEqual(new[] { Initial, DefaultValue }, (await ctx.Database.GetAppliedMigrationsAsync()).ToArray());
            Assert.IsFalse((await ctx.Database.GetPendingMigrationsAsync()).Any());
        }

        Assert.AreEqual("numeric", await TestDb.ScalarAsync(cs,
            "SELECT data_type FROM information_schema.columns WHERE table_name = 'numbers' AND column_name = 'large_number'"));
        Assert.AreEqual("1.5", await TestDb.ScalarAsync(cs,
            "SELECT column_default FROM information_schema.columns WHERE table_name = 'numbers' AND column_name = 'larger_number_optional'"));

        await using (var ctx = TestDb.Context(cs))
        {
            foreach (var v in TestDb.SampleValues)
                ctx.Numbers.Add(new Number { LargeNumber = v });                    // SecondLargeNumber left to the DB default
            ctx.Numbers.Add(new Number { LargeNumber = 7, SecondLargeNumber = TestDb.SampleValues[7] });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = TestDb.Context(cs))
        {
            var rows = await ctx.Numbers.OrderBy(n => n.Id).ToListAsync();
            Assert.AreEqual(TestDb.SampleValues.Length + 1, rows.Count);
            for (var i = 0; i < TestDb.SampleValues.Length; i++)
            {
                Assert.AreEqual(TestDb.SampleValues[i], rows[i].LargeNumber, $"row {i}");
                Assert.AreEqual(BigDecimal.Parse("1.5"), rows[i].SecondLargeNumber, $"row {i} default");
            }
            Assert.AreEqual(TestDb.SampleValues[7], rows[^1].SecondLargeNumber);
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestDb.PostgresVersions), typeof(TestDb))]
    public async Task Down_migrations_revert_and_reapply(string pg)
    {
        var cs = await TestDb.CreateDatabaseAsync(pg);
        await using var ctx = TestDb.Context(cs);
        var migrator = ctx.GetService<IMigrator>();
        const string columnCount = "SELECT count(*) FROM information_schema.columns WHERE table_name = 'numbers'";

        await migrator.MigrateAsync();
        Assert.AreEqual("3", await TestDb.ScalarAsync(cs, columnCount));

        await migrator.MigrateAsync(Initial);
        Assert.AreEqual("2", await TestDb.ScalarAsync(cs, columnCount), "DefaultValueBigDecimal Down should drop the column");

        await migrator.MigrateAsync();
        Assert.AreEqual("1.5", await TestDb.ScalarAsync(cs,
            "SELECT column_default FROM information_schema.columns WHERE table_name = 'numbers' AND column_name = 'larger_number_optional'"));

        await migrator.MigrateAsync(Migration.InitialDatabase);
        Assert.AreEqual("0", await TestDb.ScalarAsync(cs, columnCount), "initial Down should drop the table");
    }

    [TestMethod]
    [DynamicData(nameof(TestDb.PostgresVersions), typeof(TestDb))]
    public async Task Idempotent_script_applies_and_matches_migrate(string pg)
    {
        var cs = await TestDb.CreateDatabaseAsync(pg);
        string script;
        await using (var ctx = TestDb.Context(cs))
            script = ctx.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);

        StringAssert.Contains(script, "DEFAULT 1.5");

        // Apply twice: an idempotent script must be a no-op the second time.
        for (var i = 0; i < 2; i++)
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(script, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var ctx = TestDb.Context(cs))
        {
            Assert.IsFalse((await ctx.Database.GetPendingMigrationsAsync()).Any());
            ctx.Numbers.Add(new Number { LargeNumber = TestDb.SampleValues[7] });
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = TestDb.Context(cs))
        {
            var row = await ctx.Numbers.SingleAsync();
            Assert.AreEqual(TestDb.SampleValues[7], row.LargeNumber);
            Assert.AreEqual(BigDecimal.Parse("1.5"), row.SecondLargeNumber);
        }
    }

    [TestMethod]
    public void Scaffolding_with_no_model_change_produces_empty_migration()
    {
        using var ctx = TestDb.Context("Host=localhost;Database=unused");
        var migration = Scaffolder(ctx).ScaffoldMigration("NoChange", "Scaffolded", null, "C#");

        StringAssert.DoesNotMatch(migration.MigrationCode, new System.Text.RegularExpressions.Regex(@"migrationBuilder\."));
    }

    [TestMethod]
    public void Scaffolding_new_BigDecimal_column_generates_parse_literal()
    {
        using (var check = TestDb.Context("Host=localhost;Database=unused", AddFeeColumn))
            Assert.IsTrue(check.Database.HasPendingModelChanges());

        // Fresh context, like `dotnet ef migrations add`: on EF Core 10, scaffolding from a context that
        // already ran HasPendingModelChanges() diffs against an empty model (CreateTable), even without BigDecimal.
        using var ctx = TestDb.Context("Host=localhost;Database=unused", AddFeeColumn);
        var migration = Scaffolder(ctx).ScaffoldMigration("AddFee", "Scaffolded", null, "C#");

        StringAssert.Contains(migration.MigrationCode, "AddColumn<BigDecimal>(");
        StringAssert.Contains(migration.MigrationCode, "name: \"fee\"");
        StringAssert.Contains(migration.MigrationCode, "type: \"numeric\"");
        StringAssert.Contains(migration.MigrationCode, "defaultValue: Nethereum.Util.BigDecimal.Parse(\"0.000000000000000001\")");
        StringAssert.Contains(migration.SnapshotCode, ".HasDefaultValue(Nethereum.Util.BigDecimal.Parse(\"0.000000000000000001\"))");
    }

    [TestMethod]
    [DynamicData(nameof(TestDb.PostgresVersions), typeof(TestDb))]
    public async Task New_BigDecimal_column_migration_applies_on_ef8_database(string pg)
    {
        var cs = await TestDb.CreateDatabaseAsync(pg);
        await using (var ctx = TestDb.Context(cs))
        {
            await ctx.Database.MigrateAsync();
            ctx.Numbers.Add(new Number { LargeNumber = TestDb.SampleValues[6] });
            await ctx.SaveChangesAsync();
        }

        // What `dotnet ef migrations add AddFee` + `database update` would run, computed by this EF version.
        await using (var ctx = TestDb.Context(cs, AddFeeColumn))
        {
            var snapshotModel = ctx.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
            var source = ctx.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel).GetRelationalModel();
            var target = ctx.GetService<IDesignTimeModel>().Model.GetRelationalModel();
            var operations = ctx.GetService<IMigrationsModelDiffer>().GetDifferences(source, target);
            Assert.AreEqual(1, operations.Count, string.Join(", ", operations.Select(o => o.GetType().Name)));

            var commands = ctx.GetService<IMigrationsSqlGenerator>().Generate(operations, ctx.Model);
            var sql = string.Join("\n", commands.Select(c => c.CommandText));
            StringAssert.Contains(sql, "DEFAULT 0.000000000000000001");
            await ctx.GetService<IMigrationCommandExecutor>().ExecuteNonQueryAsync(commands, ctx.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalConnection>());
        }

        await using (var ctx = TestDb.Context(cs, AddFeeColumn))
        {
            ctx.Numbers.Add(new Number { LargeNumber = 1 });
            await ctx.SaveChangesAsync();
            var fees = await ctx.Numbers.OrderBy(n => n.Id).Select(n => EF.Property<BigDecimal>(n, "Fee")).ToListAsync();
            CollectionAssert.AreEqual(new[] { FeeDefault, FeeDefault }, fees, "existing row backfilled and new row defaulted");
            Assert.AreEqual(1, await ctx.Numbers.CountAsync(n => EF.Property<BigDecimal>(n, "Fee") == FeeDefault && n.LargeNumber == 1));
        }
    }

    static IMigrationsScaffolder Scaffolder(NumberContext ctx)
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(ctx);
        new NpgsqlDesignTimeServices().ConfigureDesignTimeServices(services);
        return services.BuildServiceProvider().GetRequiredService<IMigrationsScaffolder>();
    }
}
