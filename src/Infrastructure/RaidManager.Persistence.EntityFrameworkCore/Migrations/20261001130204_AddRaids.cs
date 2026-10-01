using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddRaids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Raids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SignupDeadlineUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Requirements_MaximumCharacterDataAge = table.Column<long>(type: "bigint", nullable: false),
                    Requirements_MinimumGearScore = table.Column<int>(type: "int", nullable: true),
                    Requirements_RequiresUnsavedCharacter = table.Column<bool>(type: "bit", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaidSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Availability = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LateArrivalUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RaidId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidSignups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidSignups_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Instance = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RaidId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidTargets_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupNumber = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    RaidId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RosterSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterSelections_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidSignupOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RaidSignupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidSignupOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidSignupOptions_RaidSignups_RaidSignupId",
                        column: x => x.RaidSignupId,
                        principalTable: "RaidSignups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Raids_CommunityId_StartsAtUtc",
                table: "Raids",
                columns: new[] { "CommunityId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignupOptions_RaidSignupId",
                table: "RaidSignupOptions",
                column: "RaidSignupId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidSignups_RaidId_UserId",
                table: "RaidSignups",
                columns: new[] { "RaidId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidTargets_RaidId",
                table: "RaidTargets",
                column: "RaidId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterSelections_RaidId_UserId",
                table: "RosterSelections",
                columns: new[] { "RaidId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidSignupOptions");

            migrationBuilder.DropTable(
                name: "RaidTargets");

            migrationBuilder.DropTable(
                name: "RosterSelections");

            migrationBuilder.DropTable(
                name: "RaidSignups");

            migrationBuilder.DropTable(
                name: "Raids");
        }
    }
}
