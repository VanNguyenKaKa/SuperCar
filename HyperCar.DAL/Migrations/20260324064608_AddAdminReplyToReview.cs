using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HyperCar.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminReplyToReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdminRepliedAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "Reviews",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1684));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1693));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1694));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1696));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1697));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1699));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1700));

            migrationBuilder.UpdateData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1701));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1739));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1761));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1765));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1769));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1772));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1779));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 24, 6, 46, 5, 238, DateTimeKind.Utc).AddTicks(1782));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminRepliedAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "Reviews");

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
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1218));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1247));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1259));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1270));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1279));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1289));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1299));

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 23, 4, 7, 13, 723, DateTimeKind.Utc).AddTicks(1412));
        }
    }
}
