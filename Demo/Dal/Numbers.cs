using System.ComponentModel.DataAnnotations.Schema;
using Nethereum.Util;

namespace Demo.Dal;

[Table("numbers")]
public class Number
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("large_number")]
    public BigDecimal LargeNumber { get; set; }
}
