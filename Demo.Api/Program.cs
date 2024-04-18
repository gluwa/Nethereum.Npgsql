using System.Reflection;
using Demo.Api;
using Demo.Api.Dal;
using Microsoft.EntityFrameworkCore;
using Nethereum.Npgsql.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<NumberContext>(options =>
{
    var configuration = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(configuration, o =>
    {
        o.UseBigDecimal();
        o.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
    });
});

var app = builder.Build();



app.MapGet("/", () => "Hello World!");

app.Run();
