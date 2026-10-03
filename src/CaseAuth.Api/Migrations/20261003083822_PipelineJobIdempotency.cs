using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class PipelineJobIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "ProcessingJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_CaseId_IdempotencyKey",
                table: "ProcessingJobs",
                columns: new[] { "CaseId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessingJobs_CaseId_IdempotencyKey",
                table: "ProcessingJobs");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ProcessingJobs");
        }
    }
}
