using Nethereum.Util;
using Npgsql.Internal;
using Npgsql.Internal.Postgres;

namespace Nethereum.Postgres.Npgsql;

internal class BigDecimalTypeInfoResolverFactory : PgTypeInfoResolverFactory
{
    public override IPgTypeInfoResolver? CreateArrayResolver() => new Resolver();
    public override IPgTypeInfoResolver CreateResolver() => new ArrayResolver();

    class Resolver : IPgTypeInfoResolver
    {
        protected static DataTypeName NumericDataTypeName => new("pg_catalog.numeric");

        TypeInfoMappingCollection? _mappings;
        protected TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new());

        public PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
        => Mappings.Find(type, dataTypeName, options);

        static TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddStructType<BigDecimal>(NumericDataTypeName,
                static (options, mapping, _) =>
                    mapping.CreateInfo(options, new BigDecimalConverter()));

            return mappings;
        }
    }

    class ArrayResolver : Resolver, IPgTypeInfoResolver
    {
        TypeInfoMappingCollection? _mappings;
        new TypeInfoMappingCollection Mappings => _mappings ??= AddMappings(new(base.Mappings));

        public new PgTypeInfo? GetTypeInfo(Type? type, DataTypeName? dataTypeName, PgSerializerOptions options)
            => Mappings.Find(type, dataTypeName, options);

        static TypeInfoMappingCollection AddMappings(TypeInfoMappingCollection mappings)
        {
            mappings.AddStructArrayType<BigDecimal>(NumericDataTypeName);
            return mappings;
        }
    }
}