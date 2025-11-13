using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vigia_del_rio_procesamiento.Migrations
{
    /// <inheritdoc />
    public partial class RainAlertTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertasLluvia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<Guid>(type: "uuid", nullable: true),
                    SensorNombre = table.Column<string>(type: "text", nullable: true),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<double>(type: "double precision", nullable: false),
                    AverageValue = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasLluvia", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RainAlert_Sensor_TriggeredAt",
                table: "AlertasLluvia",
                columns: new[] { "SensorId", "TriggeredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasLluvia");
        }
    }
}
