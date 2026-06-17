using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SprintPlanner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backlog_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    BusinessValue = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreferredDeveloperId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedDeveloperId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastDeveloperId = table.Column<Guid>(type: "uuid", nullable: true),
                    DependencyIds = table.Column<string>(type: "jsonb", nullable: false),
                    Tags = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backlog_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "competencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "developer_competency_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_developer_competency_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "developers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CapacityHoursPerDay = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_developers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "global_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultSprintLengthDays = table.Column<int>(type: "integer", nullable: false),
                    EffectiveWorkCoefficient = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    VelocityWindowSprints = table.Column<int>(type: "integer", nullable: false),
                    LoadWarningThreshold = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    MinSprintsForReliableStats = table.Column<int>(type: "integer", nullable: false),
                    ColdStartBeta = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    ColdStartSigmaFraction = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sprint_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ActualHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WasCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Goal = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SelectedPlanId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "competency_requirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BacklogItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinimumLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competency_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competency_requirements_backlog_items_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "backlog_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calendar_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AvailableHours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_calendar_overrides_developers_DeveloperId",
                        column: x => x.DeveloperId,
                        principalTable: "developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "developer_competencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LevelNumeric = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_developer_competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_developer_competencies_developers_DeveloperId",
                        column: x => x.DeveloperId,
                        principalTable: "developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sprint_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Weights = table.Column<string>(type: "jsonb", nullable: false),
                    Metrics = table.Column<string>(type: "jsonb", nullable: false),
                    Warnings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprint_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprint_plans_sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SprintPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    AdjustedHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ContinuityBonus = table.Column<bool>(type: "boolean", nullable: false),
                    SkillFitScore = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    IsOnCriticalPath = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_assignments_sprint_plans_SprintPlanId",
                        column: x => x.SprintPlanId,
                        principalTable: "sprint_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "global_settings",
                columns: new[] { "Id", "ColdStartBeta", "ColdStartSigmaFraction", "DefaultSprintLengthDays", "EffectiveWorkCoefficient", "LoadWarningThreshold", "MinSprintsForReliableStats", "VelocityWindowSprints" },
                values: new object[] { new Guid("00000000-0000-0000-0000-0000000005e7"), 1.0m, 0.3m, 10, 0.8m, 0.85m, 3, 5 });

            migrationBuilder.CreateIndex(
                name: "IX_backlog_items_ExternalId",
                table: "backlog_items",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_backlog_items_Status",
                table: "backlog_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_overrides_DeveloperId_Date",
                table: "calendar_overrides",
                columns: new[] { "DeveloperId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_competencies_Name",
                table: "competencies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competency_requirements_BacklogItemId",
                table: "competency_requirements",
                column: "BacklogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_developer_competencies_DeveloperId_CompetencyId",
                table: "developer_competencies",
                columns: new[] { "DeveloperId", "CompetencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_developer_competency_history_DeveloperId_CompetencyId",
                table: "developer_competency_history",
                columns: new[] { "DeveloperId", "CompetencyId" });

            migrationBuilder.CreateIndex(
                name: "IX_sprint_history_DeveloperId_TaskId",
                table: "sprint_history",
                columns: new[] { "DeveloperId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_sprint_history_SprintId",
                table: "sprint_history",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_sprint_plans_SprintId",
                table: "sprint_plans",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_task_assignments_SprintPlanId",
                table: "task_assignments",
                column: "SprintPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_overrides");

            migrationBuilder.DropTable(
                name: "competencies");

            migrationBuilder.DropTable(
                name: "competency_requirements");

            migrationBuilder.DropTable(
                name: "developer_competencies");

            migrationBuilder.DropTable(
                name: "developer_competency_history");

            migrationBuilder.DropTable(
                name: "global_settings");

            migrationBuilder.DropTable(
                name: "sprint_history");

            migrationBuilder.DropTable(
                name: "task_assignments");

            migrationBuilder.DropTable(
                name: "backlog_items");

            migrationBuilder.DropTable(
                name: "developers");

            migrationBuilder.DropTable(
                name: "sprint_plans");

            migrationBuilder.DropTable(
                name: "sprints");
        }
    }
}
