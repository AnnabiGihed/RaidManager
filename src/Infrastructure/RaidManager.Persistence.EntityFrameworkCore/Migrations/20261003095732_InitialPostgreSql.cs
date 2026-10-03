using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Realm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Class = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Race = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Faction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    GuildName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsOwnershipVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastArmorySynchronizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAddonSynchronizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastCompleteRaidSaveScanAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastIncompleteRaidSaveScanAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Audit_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordGuildId = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Realm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AdministratorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Raids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SignupDeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Requirements_MaximumCharacterDataAge = table.Column<long>(type: "bigint", nullable: false),
                    Requirements_MinimumGearScore = table.Column<int>(type: "integer", nullable: true),
                    Requirements_RequiresUnsavedCharacter = table.Column<bool>(type: "boolean", nullable: false),
                    Audit_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Audit_CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    Audit_CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Audit_ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Instance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LockoutId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ResetsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsExtended = table.Column<bool>(type: "boolean", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    GearScore = table.Column<int>(type: "integer", nullable: false),
                    TalentConfiguration_SpecializationName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TalentConfiguration_FirstTreePoints = table.Column<int>(type: "integer", nullable: false),
                    TalentConfiguration_SecondTreePoints = table.Column<int>(type: "integer", nullable: false),
                    TalentConfiguration_ThirdTreePoints = table.Column<int>(type: "integer", nullable: false),
                    TalentConfiguration_TalentCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TalentConfiguration_MajorGlyphIds = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TalentConfiguration_MinorGlyphIds = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Stats_Strength = table.Column<int>(type: "integer", nullable: false),
                    Stats_Agility = table.Column<int>(type: "integer", nullable: false),
                    Stats_Stamina = table.Column<int>(type: "integer", nullable: false),
                    Stats_Intellect = table.Column<int>(type: "integer", nullable: false),
                    Stats_Spirit = table.Column<int>(type: "integer", nullable: false),
                    Stats_Health = table.Column<int>(type: "integer", nullable: false),
                    Stats_Armor = table.Column<int>(type: "integer", nullable: false),
                    Stats_AttackPower = table.Column<int>(type: "integer", nullable: false),
                    Stats_SpellPower = table.Column<int>(type: "integer", nullable: false),
                    Stats_HitRating = table.Column<int>(type: "integer", nullable: false),
                    Stats_HitPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_CritRating = table.Column<int>(type: "integer", nullable: false),
                    Stats_CritPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_HasteRating = table.Column<int>(type: "integer", nullable: false),
                    Stats_HastePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ExpertiseRating = table.Column<int>(type: "integer", nullable: false),
                    Stats_ExpertiseMainHand = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ExpertiseOffHand = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ArmorPenetrationRating = table.Column<int>(type: "integer", nullable: false),
                    Stats_ArmorPenetrationPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_DefenseSkill = table.Column<int>(type: "integer", nullable: false),
                    Stats_DodgePercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ParryPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_BlockPercent = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    Stats_ResilienceRating = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSynchronizedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "CommunityRoleMappings",
                columns: table => new
                {
                    DiscordRoleId = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRoleMappings", x => new { x.CommunityId, x.DiscordRoleId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_CommunityRoleMappings_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityRoles_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidSignups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Availability = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LateArrivalUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    RaidId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Instance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RaidId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupNumber = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RaidId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "LoadoutGearItems",
                columns: table => new
                {
                    Slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    DisplayId = table.Column<int>(type: "integer", nullable: true),
                    ItemLink = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ItemLevel = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "RaidSignupOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaidSignupId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "IX_Communities_DiscordGuildId",
                table: "Communities",
                column: "DiscordGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRoles_CommunityId",
                table: "CommunityRoles",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Loadouts_CharacterId",
                table: "Loadouts",
                column: "CharacterId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordUserId",
                table: "Users",
                column: "DiscordUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterClaims");

            migrationBuilder.DropTable(
                name: "CharacterRaidLockouts");

            migrationBuilder.DropTable(
                name: "CommunityRoleMappings");

            migrationBuilder.DropTable(
                name: "CommunityRoles");

            migrationBuilder.DropTable(
                name: "LoadoutGearItems");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "RaidSignupOptions");

            migrationBuilder.DropTable(
                name: "RaidTargets");

            migrationBuilder.DropTable(
                name: "RosterSelections");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Communities");

            migrationBuilder.DropTable(
                name: "Loadouts");

            migrationBuilder.DropTable(
                name: "RaidSignups");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Raids");
        }
    }
}
