using System.Numerics;
using Nethereum.Util;
using Npgsql.Internal;

namespace Nethereum.Postgres.Npgsql;

public sealed class BigDecimalConverter : PgBufferedConverter<Nethereum.Util.BigDecimal>
{
    private static BigInteger BI_MAX_LONG = new BigInteger(long.MaxValue);
    private static BigInteger BI_TEN_THOUSAND = new BigInteger(10000);

    const int StackAllocByteThreshold = 256 * sizeof(uint);

    const ushort SignPositive = 0x0000;
    const ushort SignNegative = 0x4000;
    const ushort SignNan = 0xC000;
    const ushort SignPinf = 0xD000;
    const ushort SignNinf = 0xF000;

    const int BytesUpperBound = (sizeof(short) * 4) + (sizeof(short) * 20);

    public override bool CanConvert(DataFormat format, out BufferRequirements bufferRequirements)
    {
        bufferRequirements = BufferRequirements.Create(Size.CreateUpperBound(BytesUpperBound));
        return format is DataFormat.Binary;
    }

    // PostgreSQL time resolution == 1 microsecond == 10 ticks
    protected override Nethereum.Util.BigDecimal ReadCore(PgReader reader)
    {
        // header.
        var digitCount = reader.ReadInt16();
        var weight = reader.ReadInt16();
        var sign = reader.ReadUInt16();
        var scale = reader.ReadInt16();

        // digits
        var digits = stackalloc short[StackAllocByteThreshold / sizeof(short)].Slice(0, digitCount);
        foreach (ref var digit in digits)
        {
            if (reader.ShouldBuffer(sizeof(short)))
                reader.Buffer(sizeof(short));
            digit = reader.ReadInt16();
        }

        // determine sign.
        if (sign is SignNan)
            throw new InvalidCastException("Numeric NaN not supported by BigDecimal");
        if (sign is SignPinf)
            throw new InvalidCastException("Numeric Infinity not supported by BigDecimal");
        if (sign is SignNinf)
            throw new InvalidCastException("Numeric -Infinity not supported by System.Decimal");

        // Convert digits to BigInteger
        BigInteger mantissa = BigInteger.Zero;
        BigInteger baseMultiplier = 1; // Start with 1 for the least significant digit
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            BigInteger digitValue = digits[i] * baseMultiplier;
            mantissa += digitValue;
            baseMultiplier *= new BigInteger(10_000); // Increase the base multiplier for the next digit
        }

        // Adjust sign
        if (sign is SignNegative) 
        {
            mantissa = BigInteger.Negate(mantissa);
        }

        // Calculate exponent
        // The total number of digits is scale + (weight + 1) * 4 since each weight represents a chunk of 4 digits
        int totalDigits = scale + (weight + 1) * 4;
        int exponent = totalDigits - scale - digits.Length * 4;

        // Create BigDecimal instance
        var result = new BigDecimal(mantissa, exponent);
        return result;
    }

    protected override void WriteCore(PgWriter writer, Nethereum.Util.BigDecimal value)
    {
        var bytes = BigDecimalToNumbericBytes(value);
        writer.WriteBytes(bytes);
    }

    public override Size GetSize(SizeContext context, BigDecimal value, ref object? writeState)
    {
        var bytesArray = BigDecimalToNumbericBytes(value);
        return Size.Create(bytesArray.Length);
    }

    /// <summary>
    /// This was ported from the BigDecimal implementation coming from Java.
    /// </summary>
    /// <param name="nbr">BigDecimal input number</param>
    /// <returns></returns>
    public static byte[] BigDecimalToNumbericBytes(BigDecimal nbr)
    {
        var numeric = BigDecimalToPgSql(nbr);

        var bytes = new byte[8 + (2 * numeric.digits.Count)];
        int idx = 0;

        Int2(bytes, idx, numeric.digits.Count); idx += 2;
        Int2(bytes, idx, numeric.weight); idx += 2;
        Int2(bytes, idx, numeric.sign); idx += 2;
        Int2(bytes, idx, numeric.scale); idx += 2;

        for (var i = 0; i < numeric.digits.Count; i++, idx += 2)
            Int2(bytes, idx, numeric.digits[i]); 

        return bytes;
    }

    public static (short weight, short scale, ushort sign, List<short> digits) BigDecimalToPgSql(BigDecimal nbr)
    {
        var shorts = new List<short>();
        BigInteger unscaled = BigInteger.Abs(nbr.Mantissa);
        int scale = Math.Abs(nbr.Exponent);
        if (unscaled == BigInteger.Zero)
        {
            return (-1, 0, 0, shorts);
        }

        int weight = -1;
        if (scale <= 0)
        {
            if (scale < 0)
            {
                scale = Math.Abs(scale);
                weight += scale / 4;
                int mod = scale % 4;
                unscaled *= TenPower(mod);
                scale = 0;
            }

            while (unscaled > BI_MAX_LONG)
            {
                BigInteger[] pair = DivideAndRemainder(unscaled, BI_TEN_THOUSAND);
                unscaled = pair[0];
                short shortValue = (short)(pair[1]);
                if (shortValue != 0 || shorts.Count != 0)
                {
                    shorts.Add(shortValue);
                }
                ++weight;
            }
            long unscaledLong = (long)unscaled;
            do
            {
                short shortValue = (short)(unscaledLong % 10000);
                if (shortValue != 0 || shorts.Count != 0)
                {
                    shorts.Add(shortValue);
                }
                unscaledLong /= 10000L;
                ++weight;
            } while (unscaledLong != 0);
        }
        else
        {
            BigInteger[] split = DivideAndRemainder(unscaled, TenPower(scale));
            BigInteger decimals = split[1];
            BigInteger wholes = split[0];
            weight = -1;
            if (decimals != BigInteger.Zero)
            {
                int mod = scale % 4;
                int segments = scale / 4;
                if (mod != 0)
                {
                    decimals *= TenPower(4 - mod);
                    ++segments;
                }
                do
                {
                    BigInteger[] pair = DivideAndRemainder(decimals, BI_TEN_THOUSAND);
                    decimals = pair[0];
                    short shortValue = (short)(pair[1]);
                    if (shortValue != 0 || shorts.Count != 0)
                    {
                        shorts.Add(shortValue);
                    }
                    --segments;
                } while (decimals != BigInteger.Zero);

                if (wholes == BigInteger.Zero)
                {
                    weight -= segments;
                }
                else
                {
                    for (int i = 0; i < segments; i++)
                    {
                        shorts.Add((short)0);
                    }
                }
            }

            while (wholes != BigInteger.Zero)
            {
                ++weight;
                BigInteger[] pair = DivideAndRemainder(wholes, BI_TEN_THOUSAND);
                wholes = pair[0];
                short shortValue = (short)(pair[1]);
                if (shortValue != 0 || shorts.Count != 0)
                {
                    shorts.Add(shortValue);
                }
            }
        }

        var sign = nbr.Mantissa.Sign < 0 ? SignNegative : SignPositive;
        var finalScale = Math.Max(0, scale);

        // reverse them.
        shorts.Reverse();

        return ((short)weight, (short)finalScale, sign, shorts);
    }

    private static void Int2(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 1] = (byte)(value & 0xFF);
    }

    private static BigInteger[] DivideAndRemainder(BigInteger value, BigInteger divisor)
    {
        BigInteger remainder;
        BigInteger quotient = BigInteger.DivRem(value, divisor, out remainder);
        return new BigInteger[] { quotient, remainder };
    }

    private static BigInteger TenPower(int power)
    {
        return BigInteger.Pow(10, power);
    }
}
