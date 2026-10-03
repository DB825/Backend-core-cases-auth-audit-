using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncFindingScoreColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Finding.Score (and the model snapshot) have carried this column since it was added
            // for Teammate 3's weighted-sum rule score, but InitialCreate's CreateTable call for
            // "Findings" was never regenerated to include it - so every fresh database is
            // missing it, even though EF considers the model and snapshot already in sync (there
            // was nothing for `migrations add` to diff).
            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "Findings",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Findings");
        }
    }
}
