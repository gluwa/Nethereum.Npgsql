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

var numbers = new List<BigDecimal>();
for(var i = 0 ; i < 1001; i++) {

    BigDecimal number;
    if (i == 0)
        number = new BigDecimal(0);
    else if (i == 1)
        number = new BigDecimal(1);
    else
    {
        var left = new string('1', i-1);
        var right = new string('1', i-1);
        number = BigDecimal.Parse(left + "." + right);
    }

    numbers.Add(number);
}

var toAdd = numbers.Select(t => new Number {
    LargeNumber = t
}).ToList();

await numberContext.Numbers.ExecuteDeleteAsync();

// batch a certain amount of records at once.
var pager = 5;
var numberOfPages = toAdd.Count/pager + (toAdd.Count%pager == 0 ? 0 : 1);
for(var i = 0 ; i < numberOfPages; i++) {
    var subset = toAdd.Skip(i*pager).Take(pager).ToList();
    numberContext.Numbers.AddRange(subset);
    await numberContext.SaveChangesAsync();
}

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
