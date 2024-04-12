using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Nethereum.Postgres.EntityFrameworkCore;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class NpgsqlBigDecimalOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkNpgsqlBigDecimal();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Validate(IDbContextOptions options)
    {
        var internalServiceProvider = options.FindExtension<CoreOptionsExtension>()?.InternalServiceProvider;
        if (internalServiceProvider is not null)
        {
            using (var scope = internalServiceProvider.CreateScope())
            {
                if (scope.ServiceProvider.GetService<IEnumerable<IRelationalTypeMappingSourcePlugin>>()
                        ?.Any(s => s is NpgsqlBigDecimalTypeMappingSourcePlugin)
                    != true)
                {
                    throw new InvalidOperationException(
                        $"{nameof(NpgsqlBigDecimalDbContextOptionsBuilderExtensions.UseBigDecimal)} requires {nameof(NpgsqlBigDecimalServiceCollectionExtensions.AddEntityFrameworkNpgsqlBigDecimal)} to be called on the internal service provider used.");
                }
            }
        }
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        private new NpgsqlBigDecimalOptionsExtension Extension
            => (NpgsqlBigDecimalOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider
            => false;

        public override int GetServiceProviderHashCode()
            => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => true;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["Npgsql:" + nameof(NpgsqlBigDecimalDbContextOptionsBuilderExtensions.UseBigDecimal)] = "1";

        public override string LogFragment
            => "using BigDecimal ";
    }
}
