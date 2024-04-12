using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Nethereum.Npgsql.EntityFrameworkCore;

public static class NpgsqlBigDecimalDbContextOptionsBuilderExtensions
{
    /// <summary>
    ///     Configure BigDecimal of Nethereum type mappings for Entity Framework.
    /// </summary>
    /// <returns> The options builder so that further configuration can be chained. </returns>
    public static NpgsqlDbContextOptionsBuilder UseBigDecimal(
        this NpgsqlDbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder is null)
        {
            throw new ArgumentNullException(nameof(optionsBuilder));
        }

        // TODO: Global-only setup at the ADO.NET level for now, optionally allow per-connection?
#pragma warning disable CS0618 // NpgsqlConnection.GlobalTypeMapper is obsolete
        NpgsqlConnection.GlobalTypeMapper.UseBigDecimal();
#pragma warning restore CS0618

        var coreOptionsBuilder = ((IRelationalDbContextOptionsBuilderInfrastructure)optionsBuilder).OptionsBuilder;

        var extension = coreOptionsBuilder.Options.FindExtension<NpgsqlBigDecimalOptionsExtension>()
            ?? new NpgsqlBigDecimalOptionsExtension();

        ((IDbContextOptionsBuilderInfrastructure)coreOptionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }
}
