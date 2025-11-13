using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace vigia_del_rio_procesamiento.Migrations
{
    /// <inheritdoc />
    public partial class RainfallLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageValue",
                table: "AlertasLluvia");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "AlertasLluvia",
                newName: "Millimeters");

            migrationBuilder.AddColumn<string>(
                name: "Evento",
                table: "DatosMqtt",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SensorId",
                table: "AlertasLluvia",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "AlertasLluvia",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EstadosSensores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SensorNombre = table.Column<string>(type: "text", nullable: true),
                    LastCode = table.Column<string>(type: "text", nullable: false),
                    LastSumMillimeters = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosSensores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstadosSensores_SensorId",
                table: "EstadosSensores",
                column: "SensorId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadosSensores");

            migrationBuilder.DropColumn(
                name: "Evento",
                table: "DatosMqtt");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "AlertasLluvia");

            migrationBuilder.RenameColumn(
                name: "Millimeters",
                table: "AlertasLluvia",
                newName: "DurationMinutes");

            migrationBuilder.AlterColumn<Guid>(
                name: "SensorId",
                table: "AlertasLluvia",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<double>(
                name: "AverageValue",
                table: "AlertasLluvia",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
