using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaidManager.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSnapshotImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Stats_Strength",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Stamina",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Spirit",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_SpellPower",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ResilienceRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ParryPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Intellect",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_HitRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_HitPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Health",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_HasteRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_HastePercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ExpertiseRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ExpertiseOffHand",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ExpertiseMainHand",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_DodgePercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_DefenseSkill",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_CritRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_CritPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_BlockPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_AttackPower",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ArmorPenetrationRating",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ArmorPenetrationPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Armor",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Agility",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "GearScore",
                table: "Loadouts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "TalentGroup",
                table: "Loadouts",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ItemLevel",
                table: "LoadoutGearItems",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUploadAtUtc",
                table: "Companions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAddonSnapshotCapturedAtUtc",
                table: "Characters",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TalentGroup",
                table: "Loadouts");

            migrationBuilder.DropColumn(
                name: "LastUploadAtUtc",
                table: "Companions");

            migrationBuilder.DropColumn(
                name: "LastAddonSnapshotCapturedAtUtc",
                table: "Characters");

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Strength",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Stamina",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Spirit",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_SpellPower",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ResilienceRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ParryPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Intellect",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_HitRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_HitPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Health",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_HasteRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_HastePercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ExpertiseRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ExpertiseOffHand",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ExpertiseMainHand",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_DodgePercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_DefenseSkill",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_CritRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_CritPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_BlockPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_AttackPower",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_ArmorPenetrationRating",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Stats_ArmorPenetrationPercent",
                table: "Loadouts",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(9,4)",
                oldPrecision: 9,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Armor",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stats_Agility",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GearScore",
                table: "Loadouts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ItemLevel",
                table: "LoadoutGearItems",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
