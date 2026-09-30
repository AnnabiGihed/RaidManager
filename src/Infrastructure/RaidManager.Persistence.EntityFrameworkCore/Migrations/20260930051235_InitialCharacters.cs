using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Realm = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Class = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Race = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Faction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    GuildName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsOwnershipVerified = table.Column<bool>(type: "bit", nullable: false),
                    LastArmorySynchronizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAddonSynchronizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastCompleteRaidSaveScanAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastIncompleteRaidSaveScanAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Processed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterClaims_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRaidLockouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Instance = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LockoutId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ResetsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsExtended = table.Column<bool>(type: "bit", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRaidLockouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterRaidLockouts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Loadouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    GearScore = table.Column<int>(type: "int", nullable: false),
                    TalentConfiguration_SpecializationName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TalentConfiguration_FirstTreePoints = table.Column<int>(type: "int", nullable: false),
                    TalentConfiguration_SecondTreePoints = table.Column<int>(type: "int", nullable: false),
                    TalentConfiguration_ThirdTreePoints = table.Column<int>(type: "int", nullable: false),
                    TalentConfiguration_TalentCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TalentConfiguration_MajorGlyphIds = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TalentConfiguration_MinorGlyphIds = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Stats_Strength = table.Column<int>(type: "int", nullable: false),
                    Stats_Agility = table.Column<int>(type: "int", nullable: false),
                    Stats_Stamina = table.Column<int>(type: "int", nullable: false),
                    Stats_Intellect = table.Column<int>(type: "int", nullable: false),
                    Stats_Spirit = table.Column<int>(type: "int", nullable: false),
                    Stats_Health = table.Column<int>(type: "int", nullable: false),
                    Stats_Armor = table.Column<int>(type: "int", nullable: false),
                    Stats_AttackPower = table.Column<int>(type: "int", nullable: false),
                    Stats_SpellPower = table.Column<int>(type: "int", nullable: false),
                    Stats_HitRating = table.Column<int>(type: "int", nullable: false),
                    Stats_HitPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_CritRating = table.Column<int>(type: "int", nullable: false),
                    Stats_CritPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_HasteRating = table.Column<int>(type: "int", nullable: false),
                    Stats_HastePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ExpertiseRating = table.Column<int>(type: "int", nullable: false),
                    Stats_ExpertiseMainHand = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ExpertiseOffHand = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ArmorPenetrationRating = table.Column<int>(type: "int", nullable: false),
                    Stats_ArmorPenetrationPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_DefenseSkill = table.Column<int>(type: "int", nullable: false),
                    Stats_DodgePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ParryPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_BlockPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ResilienceRating = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastSynchronizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Loadouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Loadouts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoadoutGearItems",
                columns: table => new
                {
                    Slot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    DisplayId = table.Column<int>(type: "int", nullable: true),
                    ItemLink = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ItemLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadoutGearItems", x => new { x.LoadoutId, x.Slot });
                    table.ForeignKey(
                        name: "FK_LoadoutGearItems_Loadouts_LoadoutId",
                        column: x => x.LoadoutId,
                        principalTable: "Loadouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterClaims_CharacterId",
                table: "CharacterClaims",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterClaims_RequestedByUserId",
                table: "CharacterClaims",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRaidLockouts_CharacterId",
                table: "CharacterRaidLockouts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Realm_Name",
                table: "Characters",
                columns: new[] { "Realm", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loadouts_CharacterId",
                table: "Loadouts",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterClaims");

            migrationBuilder.DropTable(
                name: "CharacterRaidLockouts");

            migrationBuilder.DropTable(
                name: "LoadoutGearItems");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Loadouts");

            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
