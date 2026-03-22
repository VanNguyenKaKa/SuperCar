using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HyperCar.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShowroom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShowroomId",
                table: "TestDriveBookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Showrooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showrooms", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7718));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7777));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7779));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7780));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7782));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7783));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7785));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7786));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7844));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7870));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7875));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7952));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7956));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7960));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7963));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 12, 48, 38, 324, DateTimeKind.Utc).AddTicks(7970));

            migrationBuilder.CreateIndex(
                name: "IX_TestDriveBookings_ShowroomId",
                table: "TestDriveBookings",
                column: "ShowroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Showrooms_Name",
                table: "Showrooms",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_TestDriveBookings_Showrooms_ShowroomId",
                table: "TestDriveBookings",
                column: "ShowroomId",
                principalTable: "Showrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestDriveBookings_Showrooms_ShowroomId",
                table: "TestDriveBookings");

            migrationBuilder.DropTable(
                name: "Showrooms");

            migrationBuilder.DropIndex(
                name: "IX_TestDriveBookings_ShowroomId",
                table: "TestDriveBookings");

            migrationBuilder.DropColumn(
                name: "ShowroomId",
                table: "TestDriveBookings");

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7918));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7924));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7925));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7926));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7928));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7929));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7930));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7931));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7955));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7965));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7968));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7971));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7974));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7977));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7979));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 22, 9, 34, 58, 839, DateTimeKind.Utc).AddTicks(7981));
        }
    }
}
