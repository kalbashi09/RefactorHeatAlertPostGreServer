using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RefactorHeatAlertPostGre.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_personnel",
                columns: table => new
                {
                    admin_uid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    personnel_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passcode_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_personnel", x => x.admin_uid);
                });

            migrationBuilder.CreateTable(
                name: "sensor_registry",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sensor_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    barangay = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    baseline_temp = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    environment_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_registry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscribers",
                columns: table => new
                {
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_subscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    subscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_notified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscribers", x => x.chat_id);
                });

            migrationBuilder.CreateTable(
                name: "heat_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sensor_id = table.Column<int>(type: "integer", nullable: false),
                    recorded_temp = table.Column<int>(type: "integer", nullable: false),
                    heat_index = table.Column<int>(type: "integer", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_heat_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_heat_logs_sensor",
                        column: x => x.sensor_id,
                        principalTable: "sensor_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_auth_personnel_is_active",
                table: "auth_personnel",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_auth_personnel_personnel_id",
                table: "auth_personnel",
                column: "personnel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_heat_logs_heat_index",
                table: "heat_logs",
                column: "heat_index");

            migrationBuilder.CreateIndex(
                name: "idx_heat_logs_recorded_at",
                table: "heat_logs",
                column: "recorded_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_heat_logs_sensor_id",
                table: "heat_logs",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "idx_sensor_registry_barangay",
                table: "sensor_registry",
                column: "barangay");

            migrationBuilder.CreateIndex(
                name: "idx_sensor_registry_is_active",
                table: "sensor_registry",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_sensor_registry_sensor_code",
                table: "sensor_registry",
                column: "sensor_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_subscribers_is_subscribed",
                table: "subscribers",
                column: "is_subscribed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_personnel");

            migrationBuilder.DropTable(
                name: "heat_logs");

            migrationBuilder.DropTable(
                name: "subscribers");

            migrationBuilder.DropTable(
                name: "sensor_registry");
        }
    }
}
