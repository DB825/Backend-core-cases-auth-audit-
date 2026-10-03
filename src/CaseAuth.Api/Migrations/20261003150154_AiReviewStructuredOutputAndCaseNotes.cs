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
            // No explicit column types: the same migration runs on SQLite locally and Postgres in
            // deployment, so each provider picks its own (uuid, timestamp with time zone, ...).
            migrationBuilder.AddColumn<string>(
                name: "DraftCaseNote",
                table: "AiReviews",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyConcerns",
                table: "AiReviews",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "NextSteps",
                table: "AiReviews",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "AiReviews",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseNotes",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(nullable: false),
                    Text = table.Column<string>(nullable: false),
                    BasedOnAiReviewVersion = table.Column<int>(nullable: true),
                    UpdatedByUserId = table.Column<string>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false)
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
