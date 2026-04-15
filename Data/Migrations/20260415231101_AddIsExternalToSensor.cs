using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefactorHeatAlertPostGre.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsExternalToSensor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExternal",
                table: "sensor_registry",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExternal",
                table: "sensor_registry");
        }
    }
}
