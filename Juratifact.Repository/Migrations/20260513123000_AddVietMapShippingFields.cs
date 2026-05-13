using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juratifact.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddVietMapShippingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ShippingLatitude",
                schema: "public",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ShippingLongitude",
                schema: "public",
                table: "Orders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VietMapRefId",
                schema: "public",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingLatitude",
                schema: "public",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingLongitude",
                schema: "public",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VietMapRefId",
                schema: "public",
                table: "Orders");
        }
    }
}
