using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AllowADiscordRoleToGiveSeveralRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings",
                columns: new[] { "CommunityId", "DiscordRoleId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CommunityRoleMappings",
                table: "CommunityRoleMappings",
                columns: new[] { "CommunityId", "DiscordRoleId" });
        }
    }
}
