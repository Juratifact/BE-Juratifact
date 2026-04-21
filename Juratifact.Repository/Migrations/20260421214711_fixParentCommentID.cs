using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Juratifact.Repository.Migrations
{
    /// <inheritdoc />
    public partial class fixParentCommentID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductComments_ProductComments_ParentId",
                table: "ProductComments");

            migrationBuilder.DropIndex(
                name: "IX_ProductComments_ParentId",
                table: "ProductComments");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ProductComments");

            migrationBuilder.CreateIndex(
                name: "IX_ProductComments_ParentCommentId",
                table: "ProductComments",
                column: "ParentCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductComments_ProductComments_ParentCommentId",
                table: "ProductComments",
                column: "ParentCommentId",
                principalTable: "ProductComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductComments_ProductComments_ParentCommentId",
                table: "ProductComments");

            migrationBuilder.DropIndex(
                name: "IX_ProductComments_ParentCommentId",
                table: "ProductComments");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "ProductComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductComments_ParentId",
                table: "ProductComments",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductComments_ProductComments_ParentId",
                table: "ProductComments",
                column: "ParentId",
                principalTable: "ProductComments",
                principalColumn: "Id");
        }
    }
}
