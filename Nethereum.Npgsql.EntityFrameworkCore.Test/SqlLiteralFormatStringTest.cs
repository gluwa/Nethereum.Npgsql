using Nethereum.Util;

namespace Nethereum.Npgsql.EntityFrameworkCore.Test;

[TestClass]
public class SqlLiteralFormatStringTest
{
    [TestMethod]
    public void BigDecimalFormattingTests()
    {
        var list = new List<BigDecimal>();
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

            list.Add(number);
        }

        foreach(var number in list) {
            var mapping = new NpgsqlBigDecimalTypeMapping(typeof(BigDecimal));
            var result = mapping.GenerateSqlLiteral(number);
            var expected = number.ToString();
            Assert.AreEqual(expected, result, $"generated SQL literal did not match {number} != {result}");
        }
    }
}