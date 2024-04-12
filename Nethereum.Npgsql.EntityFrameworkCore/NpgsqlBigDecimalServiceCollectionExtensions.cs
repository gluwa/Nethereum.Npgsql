using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Nethereum.Npgsql.EntityFrameworkCore;


public static class NpgsqlBigDecimalServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the services required for BigDecimal support in the Npgsql provider for Entity Framework.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkNpgsqlBigDecimal(
        this IServiceCollection serviceCollection)
    {

        new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<IRelationalTypeMappingSourcePlugin, NpgsqlBigDecimalTypeMappingSourcePlugin>();

        return serviceCollection;
    }
}
