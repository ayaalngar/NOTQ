using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NOTQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentWordsFoxAndTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Words",
                columns: new[] { "Id", "CategoryLetter", "CreatedAt", "ImageUrl", "Type", "Word" },
                values: new object[,]
                {
                    { 8, "ث", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/words/fox.png", "Word", "ثعلب" },
                    { 9, "ش", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/words/tree.png", "Word", "شجرة" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Words",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Words",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
