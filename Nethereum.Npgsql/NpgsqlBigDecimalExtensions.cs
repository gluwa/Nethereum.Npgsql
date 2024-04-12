using Npgsql.TypeMapping;

namespace Nethereum.Npgsql;

public static class NpgsqlBigDecimalExtensions
{
    /// <summary>
    /// Sets up BigDecimal of Nethereum mappings for the PostgreSQL date/time types.
    /// </summary>
    /// <param name="mapper">The type mapper to set up (global or connection-specific)</param>
    public static INpgsqlTypeMapper UseBigDecimal(this INpgsqlTypeMapper mapper)
    {
        mapper.AddTypeInfoResolverFactory(new BigDecimalTypeInfoResolverFactory());
        return mapper;
    }
}


