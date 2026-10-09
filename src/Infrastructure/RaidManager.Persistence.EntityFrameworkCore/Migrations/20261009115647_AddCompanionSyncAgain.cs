using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanionSyncAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SyncAgainRequestedAtUtc",
                table: "Companions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyncAgainRequestedAtUtc",
                table: "Companions");
        }
    }
}
