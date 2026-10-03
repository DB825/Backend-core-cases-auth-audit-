using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirmId = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirmId = table.Column<string>(nullable: false),
                    ActorUserId = table.Column<string>(nullable: false),
                    ActorUsername = table.Column<string>(nullable: false),
                    Action = table.Column<string>(nullable: false),
                    Outcome = table.Column<string>(nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(nullable: false),
                    AiReviewVersion = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirmId = table.Column<string>(nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(nullable: false),
                    CreatedByUserId = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(nullable: false),
                    ModelVersion = table.Column<string>(nullable: false),
                    Recommendation = table.Column<string>(nullable: false),
                    Rationale = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiReviews_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(nullable: false),
                    ContentType = table.Column<string>(nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<string>(nullable: false),
                    StorageKey = table.Column<string>(nullable: false),
                    UploadedByUserId = table.Column<string>(nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(nullable: false),
                    Source = table.Column<string>(nullable: false),
                    Code = table.Column<string>(nullable: false),
                    Message = table.Column<string>(nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Findings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(nullable: false),
                    Status = table.Column<string>(nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingJobs_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(nullable: false),
                    AiReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedByUserId = table.Column<string>(nullable: false),
                    IdempotencyKey = table.Column<string>(nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decisions_AiReviews_AiReviewId",
                        column: x => x.AiReviewId,
                        principalTable: "AiReviews",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Decisions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(nullable: false),
                    FieldValue = table.Column<string>(nullable: false),
                    Confidence = table.Column<double>(nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractedFields_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedFieldFinding",
                columns: table => new
                {
                    FindingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceFieldsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedFieldFinding", x => new { x.FindingId, x.SourceFieldsId });
                    table.ForeignKey(
                        name: "FK_ExtractedFieldFinding_ExtractedFields_SourceFieldsId",
                        column: x => x.SourceFieldsId,
                        principalTable: "ExtractedFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExtractedFieldFinding_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiReviews_CaseId_Version",
                table: "AiReviews",
                columns: new[] { "CaseId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_FirmId",
                table: "Applicants",
                column: "FirmId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_CaseId",
                table: "AuditEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_FirmId",
                table: "AuditEvents",
                column: "FirmId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ApplicantId",
                table: "Cases",
                column: "ApplicantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_FirmId",
                table: "Cases",
                column: "FirmId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_AiReviewId",
                table: "Decisions",
                column: "AiReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_CaseId_IdempotencyKey",
                table: "Decisions",
                columns: new[] { "CaseId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CaseId",
                table: "Documents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedFieldFinding_SourceFieldsId",
                table: "ExtractedFieldFinding",
                column: "SourceFieldsId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedFields_DocumentId",
                table: "ExtractedFields",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_CaseId",
                table: "Findings",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_CaseId",
                table: "ProcessingJobs",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingJobs_Status",
                table: "ProcessingJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropTable(
                name: "ExtractedFieldFinding");

            migrationBuilder.DropTable(
                name: "ProcessingJobs");

            migrationBuilder.DropTable(
                name: "AiReviews");

            migrationBuilder.DropTable(
                name: "ExtractedFields");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "Applicants");
        }
    }
}
