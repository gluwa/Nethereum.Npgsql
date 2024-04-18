using System.ComponentModel.DataAnnotations.Schema;
using Nethereum.Util;

namespace Demo.Api.Dal;

[Table("numbers")]
public class Number
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("large_number")]
    public BigDecimal LargeNumber { get; set; }

    [Column("larger_number_optional")]

    public BigDecimal? SecondLargeNumber { get; set; }
}
