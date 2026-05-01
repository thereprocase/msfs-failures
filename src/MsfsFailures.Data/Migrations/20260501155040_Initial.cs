using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MsfsFailures.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accelerators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Variable = table.Column<string>(type: "TEXT", nullable: false),
                    FormulaJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accelerators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelRefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    SimMatchRulesJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelRefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Airframes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tail = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ModelRefId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalHobbsHours = table.Column<double>(type: "REAL", nullable: false),
                    TotalCycles = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airframes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Airframes_ModelRefs_ModelRefId",
                        column: x => x.ModelRefId,
                        principalTable: "ModelRefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModelRefId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MtbfHours = table.Column<double>(type: "REAL", nullable: false),
                    WearCurveJson = table.Column<string>(type: "TEXT", nullable: false),
                    ConsumableKind = table.Column<int>(type: "INTEGER", nullable: false),
                    ReplaceIntervalHours = table.Column<double>(type: "REAL", nullable: true),
                    ReplaceIntervalCycles = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentTemplates_ModelRefs_ModelRefId",
                        column: x => x.ModelRefId,
                        principalTable: "ModelRefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VarBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModelRefId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LogicalName = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Expression = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VarBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VarBindings_ModelRefs_ModelRefId",
                        column: x => x.ModelRefId,
                        principalTable: "ModelRefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consumables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AirframeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<double>(type: "REAL", nullable: false),
                    Capacity = table.Column<double>(type: "REAL", nullable: false),
                    LastTopUpAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consumables_Airframes_AirframeId",
                        column: x => x.AirframeId,
                        principalTable: "Airframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AirframeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    ComponentsTouchedJson = table.Column<string>(type: "TEXT", nullable: false),
                    HoursAtAction = table.Column<double>(type: "REAL", nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceActions_Airframes_AirframeId",
                        column: x => x.AirframeId,
                        principalTable: "Airframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AirframeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    HobbsStart = table.Column<double>(type: "REAL", nullable: false),
                    HobbsEnd = table.Column<double>(type: "REAL", nullable: true),
                    MaxG = table.Column<double>(type: "REAL", nullable: false),
                    HardLandings = table.Column<int>(type: "INTEGER", nullable: false),
                    OvertempEventsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Airframes_AirframeId",
                        column: x => x.AirframeId,
                        principalTable: "Airframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AirframeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hours = table.Column<double>(type: "REAL", nullable: false),
                    Cycles = table.Column<int>(type: "INTEGER", nullable: false),
                    Wear = table.Column<double>(type: "REAL", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    LastServicedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    InstalledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_Airframes_AirframeId",
                        column: x => x.AirframeId,
                        principalTable: "Airframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Components_ComponentTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComponentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FailureModes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SimBindingKind = table.Column<int>(type: "INTEGER", nullable: false),
                    SimBindingPayload = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    RepairHours = table.Column<double>(type: "REAL", nullable: false),
                    MelDeferrable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FailureModes_ComponentTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ComponentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Squawks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AirframeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FailureModeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Opened = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeferredUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    HoursAtOpen = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Squawks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Squawks_Airframes_AirframeId",
                        column: x => x.AirframeId,
                        principalTable: "Airframes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Squawks_FailureModes_FailureModeId",
                        column: x => x.FailureModeId,
                        principalTable: "FailureModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accelerators_Category",
                table: "Accelerators",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Airframes_ModelRefId",
                table: "Airframes",
                column: "ModelRefId");

            migrationBuilder.CreateIndex(
                name: "IX_Airframes_Tail",
                table: "Airframes",
                column: "Tail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Components_AirframeId",
                table: "Components",
                column: "AirframeId");

            migrationBuilder.CreateIndex(
                name: "IX_Components_TemplateId",
                table: "Components",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTemplates_ModelRefId",
                table: "ComponentTemplates",
                column: "ModelRefId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_AirframeId",
                table: "Consumables",
                column: "AirframeId");

            migrationBuilder.CreateIndex(
                name: "IX_FailureModes_TemplateId",
                table: "FailureModes",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceActions_AirframeId",
                table: "MaintenanceActions",
                column: "AirframeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AirframeId",
                table: "Sessions",
                column: "AirframeId");

            migrationBuilder.CreateIndex(
                name: "IX_Squawks_AirframeId_Status",
                table: "Squawks",
                columns: new[] { "AirframeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Squawks_FailureModeId",
                table: "Squawks",
                column: "FailureModeId");

            migrationBuilder.CreateIndex(
                name: "IX_VarBindings_ModelRefId_LogicalName",
                table: "VarBindings",
                columns: new[] { "ModelRefId", "LogicalName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accelerators");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropTable(
                name: "Consumables");

            migrationBuilder.DropTable(
                name: "MaintenanceActions");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Squawks");

            migrationBuilder.DropTable(
                name: "VarBindings");

            migrationBuilder.DropTable(
                name: "Airframes");

            migrationBuilder.DropTable(
                name: "FailureModes");

            migrationBuilder.DropTable(
                name: "ComponentTemplates");

            migrationBuilder.DropTable(
                name: "ModelRefs");
        }
    }
}
