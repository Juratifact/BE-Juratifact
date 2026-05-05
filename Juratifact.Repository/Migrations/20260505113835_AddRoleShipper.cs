using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juratifact.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleShipper : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "Shipper", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));
        }
    }
}
