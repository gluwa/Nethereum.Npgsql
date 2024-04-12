using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Data;
using Nethereum.Util;
using Microsoft.Extensions.Configuration;
using Nethereum.Npgsql.EntityFrameworkCore;
using Demo.Dal;

var configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddUserSecrets("c87cdc73-d8a1-4ee9-b28d-073d4699d58d");
var configuration = configurationBuilder.Build();

var connectionString = configuration["ConnectionString"];

var services = new ServiceCollection();
services.AddDbContext<NumberContext>(options =>
{
    options.UseNpgsql(connectionString, o =>
    {
        o.UseBigDecimal();
    });
});

var serviceProvider = services.BuildServiceProvider();
var scope = serviceProvider.CreateScope();
var numberContext = scope.ServiceProvider.GetRequiredService<NumberContext>();

var numbers = new List<BigDecimal>() {
    BigDecimal.Parse("0"),
    BigDecimal.Parse("1"),
    BigDecimal.Parse("0.1"),
    BigDecimal.Parse("0.01"),
    BigDecimal.Parse("0.001"),
    BigDecimal.Parse("0.0001"),
    BigDecimal.Parse("0.00001"),
    BigDecimal.Parse("0.00001"),
    BigDecimal.Parse("0.000001"),
    BigDecimal.Parse("0.0000001"),
    BigDecimal.Parse("0.00000001"),
    BigDecimal.Parse("0.000000001"),
    BigDecimal.Parse("0.0000000001"),
    BigDecimal.Parse("0.00000000001"),
    BigDecimal.Parse("-0.1"),
    BigDecimal.Parse("-0.01"),
    BigDecimal.Parse("-0.001"),
    BigDecimal.Parse("-0.0001"),
    BigDecimal.Parse("-0.00001"),
    BigDecimal.Parse("-0.00001"),
    BigDecimal.Parse("-0.000001"),
    BigDecimal.Parse("-0.0000001"),
    BigDecimal.Parse("-0.00000001"),
    BigDecimal.Parse("-0.000000001"),
    BigDecimal.Parse("-0.0000000001"),
    BigDecimal.Parse("-0.00000000001"),
    BigDecimal.Parse("1.1"),
    BigDecimal.Parse("11.11"),
    BigDecimal.Parse("111.111"),
    BigDecimal.Parse("1111.1111"),
    BigDecimal.Parse("11111.11111"),
    BigDecimal.Parse("111111.111111"),
    BigDecimal.Parse("1111111.1111111"),
    BigDecimal.Parse("11111111.11111111"),
    BigDecimal.Parse("111111111.111111111"),
    BigDecimal.Parse("1111111111.1111111111"),
    BigDecimal.Parse("11111111111.11111111111"),
    BigDecimal.Parse("111111111111.111111111111"),
    BigDecimal.Parse("1111111111111.1111111111111"),
    BigDecimal.Parse("11111111111111.11111111111111"),
    BigDecimal.Parse("111111111111111.111111111111111"),
    BigDecimal.Parse("1111111111111111.1111111111111111"),
    BigDecimal.Parse("11111111111111111.11111111111111111"),
    BigDecimal.Parse("111111111111111111.111111111111111111"),
    BigDecimal.Parse("100000000000000000000000000000000000000000000000000000000000000"),
    BigDecimal.Parse("100000000000000000000000000000000000000000000000000000000000000.12321323131313131232131312312")
};

var toAdd = numbers.Select(t => new Number {
    LargeNumber = t
}).ToList();

await numberContext.Numbers.ExecuteDeleteAsync();
numberContext.Numbers.AddRange(toAdd);
await numberContext.SaveChangesAsync();

var afterAdded = await numberContext.Numbers.ToListAsync();
if (afterAdded.Count != numbers.Count)
    throw new Exception("Did not add the same amount of numbers");

for(var i = 0 ; i < numbers.Count; i++) {
    if (afterAdded[i].LargeNumber != numbers[i])
        throw new Exception($"Number added at index {i} ({afterAdded[i].LargeNumber}) does not match {numbers[i]}");
} 

var connection = new NpgsqlConnection(connectionString);
connection.Open();
var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM numbers";
var reader = command.ExecuteReader();


while(reader.Read())
{
   var value = reader.GetFieldValue<BigDecimal>("large_number");
   Console.WriteLine($"Id: {reader.GetInt64("id")}, Large Number: {value}");
}

reader.Close();
connection.Close();
