using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <summary>Gives each community its own roles with permissions, and points the Discord role mappings at them (#312).</summary>
    /// <remarks>
    /// Existing communities get the Officer and Raid leader presets with the permissions they had (owner decision on
    /// #308), and every existing mapping keeps giving the same role, so nothing changes for members.
    /// </remarks>
    public partial class GiveCommunitiesTheirOwnRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Permissions = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CommunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_CommunityRoles_CommunityId",
                table: "CommunityRoles",
                column: "CommunityId");

            // The presets: Officer allows the four raid permissions (15), Raid leader raids, rosters and raid night (7).
            migrationBuilder.Sql(
                "INSERT INTO CommunityRoles (Id, Name, Permissions, Position, CommunityId) SELECT NEWID(), N'Officer', 15, 1, Id FROM Communities;");
            migrationBuilder.Sql(
                "INSERT INTO CommunityRoles (Id, Name, Permissions, Position, CommunityId) SELECT NEWID(), N'Raid leader', 7, 2, Id FROM Communities;");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "CommunityRoleMappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE mapping SET RoleId = role.Id FROM CommunityRoleMappings AS mapping "
                + "JOIN CommunityRoles AS role ON role.CommunityId = mapping.CommunityId "
                + "AND role.Position = CASE mapping.Role WHEN 'Officer' THEN 1 WHEN 'RaidLeader' THEN 2 END;");
            migrationBuilder.Sql("DELETE FROM CommunityRoleMappings WHERE RoleId IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "CommunityRoleMappings");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "CommunityRoleMappings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings",
                columns: new[] { "CommunityId", "DiscordRoleId", "RoleId" });
        }

        /// <inheritdoc />
        /// <remarks>Mappings to roles other than the two presets can't be expressed before this change and are dropped.</remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "CommunityRoleMappings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE mapping SET Role = CASE role.Position WHEN 1 THEN 'Officer' WHEN 2 THEN 'RaidLeader' END "
                + "FROM CommunityRoleMappings AS mapping JOIN CommunityRoles AS role ON role.Id = mapping.RoleId;");
            migrationBuilder.Sql("DELETE FROM CommunityRoleMappings WHERE Role IS NULL;");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "CommunityRoleMappings");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "CommunityRoleMappings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings",
                columns: new[] { "CommunityId", "DiscordRoleId", "Role" });

            migrationBuilder.DropTable(
                name: "CommunityRoles");
        }
    }
}
