using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using Nethereum.Util;

namespace Nethereum.Postgres.EntityFrameworkCore;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class NpgsqlBigDecimalTypeMappingSourcePlugin : IRelationalTypeMappingSourcePlugin
{
    static NpgsqlBigDecimalTypeMappingSourcePlugin()
    {

    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ConcurrentDictionary<string, RelationalTypeMapping[]> StoreTypeMappings { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual ConcurrentDictionary<Type, RelationalTypeMapping> ClrTypeMappings { get; }

    #region TypeMapping

    #endregion

    /// <summary>
    ///     Constructs an instance of the <see cref="NpgsqlBigDecimalTypeMappingSourcePlugin" /> class.
    /// </summary>
    public NpgsqlBigDecimalTypeMappingSourcePlugin(ISqlGenerationHelper sqlGenerationHelper)
    {
        var mapping = new NpgsqlBigDecimalTypeMapping(typeof(BigDecimal));
        var storeTypeMappings = new Dictionary<string, RelationalTypeMapping[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "numeric", new[] { mapping } }
        };

        var clrTypeMappings = new Dictionary<Type, RelationalTypeMapping>
        {
            { typeof(BigDecimal), mapping }
        };

        StoreTypeMappings = new ConcurrentDictionary<string, RelationalTypeMapping[]>(storeTypeMappings, StringComparer.OrdinalIgnoreCase);
        ClrTypeMappings = new ConcurrentDictionary<Type, RelationalTypeMapping>(clrTypeMappings);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo mappingInfo)
        => FindBaseMapping(mappingInfo)?.Clone(mappingInfo);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual RelationalTypeMapping? FindBaseMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        var clrType = mappingInfo.ClrType;
        var storeTypeName = mappingInfo.StoreTypeName;
        var storeTypeNameBase = mappingInfo.StoreTypeNameBase;

        if (storeTypeName is not null)
        {
            if (StoreTypeMappings.TryGetValue(storeTypeName, out var mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }

            if (StoreTypeMappings.TryGetValue(storeTypeNameBase!, out mappings))
            {
                if (clrType is null)
                {
                    return mappings[0];
                }

                foreach (var m in mappings)
                {
                    if (m.ClrType == clrType)
                    {
                        return m;
                    }
                }

                return null;
            }
        }

        if (clrType is not null)
        {
            if (ClrTypeMappings.TryGetValue(clrType, out var mapping))
            {
                return mapping;
            }
        }

        return null;
    }
}
