using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AiReviewStructuredOutputAndCaseNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftCaseNote",
                table: "AiReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyConcerns",
                table: "AiReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "NextSteps",
                table: "AiReviews",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AiReviews",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseNotes",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    BasedOnAiReviewVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseNotes", x => x.CaseId);
                    table.ForeignKey(
                        name: "FK_CaseNotes_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseNotes");

            migrationBuilder.DropColumn(
                name: "DraftCaseNote",
                table: "AiReviews");

            migrationBuilder.DropColumn(
                name: "KeyConcerns",
                table: "AiReviews");

            migrationBuilder.DropColumn(
                name: "NextSteps",
                table: "AiReviews");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "AiReviews");
        }
    }
}
