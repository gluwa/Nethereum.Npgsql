using Nethereum.Util;

namespace Nethereum.Postgres.Npgsql.Test;

[TestClass]
public class BigDecimalDriverTests
{
    [DataTestMethod]  
    [DataRow("0", (short)0, (short)-1, (ushort)0, (short)0, new short[] {})]  
    [DataRow("1", (short)1, (short)0, (ushort)0, (short)0, new short[] { 1 })]  
    [DataRow("-1", (short)1, (short)0, (ushort)16384, (short)0, new short[] { 1 })]  
    [DataRow("0.1", (short)1, (short)-1, (ushort)0, (short)1, new short[] { 1000 })]  
    [DataRow("0.01", (short)1, (short)-1, (ushort)0, (short)2, new short[] { 100 })]  
    [DataRow("0.111", (short)1, (short)-1, (ushort)0, (short)3, new short[] { 1110 })]  
    [DataRow("0.1111", (short)1, (short)-1, (ushort)0, (short)4, new short[] { 1111 })]  
    [DataRow("0.11111", (short)2, (short)-1, (ushort)0, (short)5, new short[] { 1111, 1000 })]  
    [DataRow("0.111111", (short)2, (short)-1, (ushort)0, (short)6, new short[] { 1111, 1100 })]  
    [DataRow("0.1111111", (short)2, (short)-1, (ushort)0, (short)7, new short[] { 1111, 1110 })]  
    [DataRow("0.11111111", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1111 })]  
    [DataRow("0.11111112", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1112 })]  
    [DataRow("0.11111113", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1113 })]  
    [DataRow("0.11111114", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1114 })]  
    [DataRow("0.11111115", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1115 })]  
    [DataRow("0.11111116", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1116 })]  
    [DataRow("0.11111117", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1117 })]  
    [DataRow("0.11111118", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1118 })]  
    [DataRow("0.11111119", (short)2, (short)-1, (ushort)0, (short)8, new short[] { 1111, 1119 })]  
    [DataRow("1.1", (short)2, (short)0, (ushort)0, (short)1, new short[] { 1, 1000 })]
    [DataRow("11.11", (short)2, (short)0, (ushort)0, (short)2, new short[] { 11, 1100 })]
    [DataRow("111.111", (short)2, (short)0, (ushort)0, (short)3, new short[] { 111, 1110 })]
    [DataRow("1111.1111", (short)2, (short)0, (ushort)0, (short)4, new short[] { 1111, 1111 })]
    [DataRow("11111.11111", (short)4, (short)1, (ushort)0, (short)5, new short[] { 1, 1111, 1111, 1000 })]
    [DataRow("111111.111111", (short)4, (short)1, (ushort)0, (short)6, new short[] { 11, 1111, 1111, 1100 })]
    [DataRow("1111111.1111111", (short)4, (short)1, (ushort)0, (short)7, new short[] { 111, 1111, 1111, 1110 })]
    [DataRow("11111111.11111111", (short)4, (short)1, (ushort)0, (short)8, new short[] { 1111, 1111, 1111, 1111 })]
    [DataRow("0.1001", (short)1, (short)-1, (ushort)0, (short)4, new short[] { 1001 })]
    [DataRow("123412341234.12341234", (short)5, (short)2, (ushort)0, (short)8, new short[] { 1234, 1234, 1234, 1234, 1234 })]
    [DataRow("1234123412345.12341234", (short)6, (short)3, (ushort)0, (short)8, new short[] { 1, 2341, 2341, 2345, 1234, 1234 })]
    public void TestConversionBigDecimalToNumeric(string raw, short len, short weight, ushort sign, short scale, short[] digits) 
    {
        var bigDecimal = BigDecimal.Parse(raw);
        var numeric = BigDecimalConverter.BigDecimalToPgSql(bigDecimal);
        Assert.AreEqual(len, numeric.digits.Count, "Len dosen't match");
        Assert.AreEqual(weight, numeric.weight, "weight does not match");
        Assert.AreEqual(sign, numeric.sign, "Sign does not match");
        Assert.AreEqual(scale, numeric.scale, "Scale does not match");
        CollectionAssert.AreEqual(digits, numeric.digits, "Digits does not match");
    }

    [TestMethod]
    public void ZeroToBytes() 
    {
        var expected = new byte[] { 0, 0, 255, 255, 0, 0, 0, 0 };
        var zero = new BigDecimal(0);
        var result = BigDecimalConverter.BigDecimalToNumbericBytes(zero);
        CollectionAssert.AreEqual(expected, result); 
    }
}