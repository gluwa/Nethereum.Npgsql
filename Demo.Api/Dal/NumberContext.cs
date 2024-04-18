using Microsoft.EntityFrameworkCore;
using Nethereum.Util;

namespace Demo.Api.Dal;

public class NumberContext : DbContext
{
    public NumberContext() : base()
    {

    }

    public NumberContext(DbContextOptions options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Number>()
            .Property(e => e.LargeNumber);

        modelBuilder.Entity<Number>()
            .Property(e => e.SecondLargeNumber)
            .HasDefaultValue(BigDecimal.Parse("1.5"));
    }

    public DbSet<Number> Numbers { get; set; }
}