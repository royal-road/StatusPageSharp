using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StatusPageSharp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyIncidentCountRollup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncidentCount",
                table: "DailyServiceRollups",
                type: "integer",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IncidentCount", table: "DailyServiceRollups");
        }
    }
}
