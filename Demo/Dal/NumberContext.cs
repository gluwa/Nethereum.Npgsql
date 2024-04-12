using Microsoft.EntityFrameworkCore;

namespace Demo.Dal;

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
            .Property(e => e.LargeNumber)
            .HasConversion(
            x => x,
            x => x);
    }

    public DbSet<Number> Numbers { get; set; }
}