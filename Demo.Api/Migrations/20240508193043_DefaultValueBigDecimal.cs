using Microsoft.EntityFrameworkCore.Migrations;
using Nethereum.Util;

#nullable disable

namespace Demo.Api.Migrations
{
    /// <inheritdoc />
    public partial class DefaultValueBigDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<BigDecimal>(
                name: "larger_number_optional",
                table: "numbers",
                type: "numeric",
                nullable: true,
                defaultValue: Nethereum.Util.BigDecimal.Parse("1.5"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "larger_number_optional",
                table: "numbers");
        }
    }
}
