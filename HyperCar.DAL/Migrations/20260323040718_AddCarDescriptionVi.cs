using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HyperCar.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCarDescriptionVi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionVi",
                table: "Cars",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1091));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1097));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1101));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1105));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1110));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1113));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1117));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1218), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1247), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1259), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1270), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1279), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1289), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1299), null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedDate", "DescriptionVi" },
                values: new object[] { new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1412), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionVi",
                table: "Cars");

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
        }
    }
}
