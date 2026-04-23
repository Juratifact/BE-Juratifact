using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juratifact.Repository.Migrations
{
    /// <inheritdoc />
    public partial class addFieldAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000001"),
                column: "Address",
                value: "123 Admin Street, Hometown, Country");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000002"),
                column: "Address",
                value: "123 Main Street, Hometown, Country");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Users");
        }
    }
}
