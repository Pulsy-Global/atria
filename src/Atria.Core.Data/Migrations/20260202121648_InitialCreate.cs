using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Atria.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NetworkId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    StartBlock = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    EndBlock = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    BlockDelay = table.Column<int>(type: "integer", nullable: false),
                    IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorHandling = table.Column<int>(type: "integer", nullable: false),
                    FilterPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FunctionPath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SearchContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Config = table.Column<string>(type: "jsonb", nullable: false),
                    SearchContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outputs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deploys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deploys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deploys_Feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedStatusChanges_Feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedOutputs_Feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedOutputs_Outputs_OutputId",
                        column: x => x.OutputId,
                        principalTable: "Outputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedTags_Feeds_FeedId",
                        column: x => x.FeedId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutputTags_Outputs_OutputId",
                        column: x => x.OutputId,
                        principalTable: "Outputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutputTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeployStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeployId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<int>(type: "integer", nullable: false),
                    ToStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeployStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeployStatusChanges_Deploys_DeployId",
                        column: x => x.DeployId,
                        principalTable: "Deploys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deploys_CreatedAt",
                table: "Deploys",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Deploys_FeedId",
                table: "Deploys",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Deploys_FeedId_Version",
                table: "Deploys",
                columns: new[] { "FeedId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_DeployStatusChanges_CreatedAt",
                table: "DeployStatusChanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeployStatusChanges_DeployId",
                table: "DeployStatusChanges",
                column: "DeployId");

            migrationBuilder.CreateIndex(
                name: "IX_DeployStatusChanges_DeployId_CreatedAt",
                table: "DeployStatusChanges",
                columns: new[] { "DeployId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedOutputs_FeedId_OutputId",
                table: "FeedOutputs",
                columns: new[] { "FeedId", "OutputId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedOutputs_OutputId",
                table: "FeedOutputs",
                column: "OutputId");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_Name",
                table: "Feeds",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_NetworkId",
                table: "Feeds",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_SearchContent",
                table: "Feeds",
                column: "SearchContent")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_Status",
                table: "Feeds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FeedStatusChanges_CreatedAt",
                table: "FeedStatusChanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeedStatusChanges_FeedId",
                table: "FeedStatusChanges",
                column: "FeedId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedStatusChanges_FeedId_CreatedAt",
                table: "FeedStatusChanges",
                columns: new[] { "FeedId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeedTags_FeedId_TagId",
                table: "FeedTags",
                columns: new[] { "FeedId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedTags_TagId",
                table: "FeedTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_SearchContent",
                table: "Outputs",
                column: "SearchContent")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Outputs_Type",
                table: "Outputs",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_OutputTags_OutputId_TagId",
                table: "OutputTags",
                columns: new[] { "OutputId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputTags_TagId",
                table: "OutputTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name_Type",
                table: "Tags",
                columns: new[] { "Name", "Type" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Type",
                table: "Tags",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeployStatusChanges");

            migrationBuilder.DropTable(
                name: "FeedOutputs");

            migrationBuilder.DropTable(
                name: "FeedStatusChanges");

            migrationBuilder.DropTable(
                name: "FeedTags");

            migrationBuilder.DropTable(
                name: "OutputTags");

            migrationBuilder.DropTable(
                name: "Deploys");

            migrationBuilder.DropTable(
                name: "Outputs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Feeds");
        }
    }
}
