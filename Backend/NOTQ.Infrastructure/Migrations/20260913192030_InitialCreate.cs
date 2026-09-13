using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NOTQ.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    AvatarId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Word = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CategoryLetter = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChildId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TotalWords = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NeedsPracticeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WordId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    AudioUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Prediction = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Confidence = table.Column<double>(type: "REAL", nullable: true),
                    IssueType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DetectedWord = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FeedbackType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FeedbackMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attempts_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Words",
                columns: new[] { "Id", "CategoryLetter", "CreatedAt", "ImageUrl", "Type", "Word" },
                values: new object[,]
                {
                    { 1, "ك", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/letters/kaf.png", "Letter", "ك" },
                    { 2, "ر", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/letters/raa.png", "Letter", "ر" },
                    { 3, "س", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/letters/seen.png", "Letter", "س" },
                    { 4, "ش", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/letters/sheen.png", "Letter", "ش" },
                    { 5, "ك", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/words/dog.png", "Word", "كلب" },
                    { 6, "ق", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/words/monkey.png", "Word", "قرد" },
                    { 7, "س", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/assets/words/lion.png", "Word", "أسد" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_SessionId",
                table: "Attempts",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_WordId",
                table: "Attempts",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ChildId",
                table: "Sessions",
                column: "ChildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempts");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropTable(
                name: "Children");
        }
    }
}
