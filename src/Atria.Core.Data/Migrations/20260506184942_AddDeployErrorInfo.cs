using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atria.Core.Data.Migrations;

/// <inheritdoc />
public partial class AddDeployErrorInfo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "CurrentDeployId",
            table: "Feeds",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErrorCode",
            table: "Deploys",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErrorMessage",
            table: "Deploys",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ErrorOccurredAt",
            table: "Deploys",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ErrorSource",
            table: "Deploys",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Feeds_CurrentDeployId",
            table: "Feeds",
            column: "CurrentDeployId");

        migrationBuilder.AddForeignKey(
            name: "FK_Feeds_Deploys_CurrentDeployId",
            table: "Feeds",
            column: "CurrentDeployId",
            principalTable: "Deploys",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Feeds_Deploys_CurrentDeployId",
            table: "Feeds");

        migrationBuilder.DropIndex(
            name: "IX_Feeds_CurrentDeployId",
            table: "Feeds");

        migrationBuilder.DropColumn(
            name: "CurrentDeployId",
            table: "Feeds");

        migrationBuilder.DropColumn(
            name: "ErrorCode",
            table: "Deploys");

        migrationBuilder.DropColumn(
            name: "ErrorMessage",
            table: "Deploys");

        migrationBuilder.DropColumn(
            name: "ErrorOccurredAt",
            table: "Deploys");

        migrationBuilder.DropColumn(
            name: "ErrorSource",
            table: "Deploys");
    }
}
