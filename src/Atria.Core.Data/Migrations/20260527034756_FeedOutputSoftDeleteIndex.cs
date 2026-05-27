using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atria.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class FeedOutputSoftDeleteIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedOutputs_FeedId_OutputId",
                table: "FeedOutputs");

            migrationBuilder.CreateIndex(
                name: "IX_FeedOutputs_FeedId_OutputId",
                table: "FeedOutputs",
                columns: new[] { "FeedId", "OutputId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeedOutputs_FeedId_OutputId",
                table: "FeedOutputs");

            migrationBuilder.CreateIndex(
                name: "IX_FeedOutputs_FeedId_OutputId",
                table: "FeedOutputs",
                columns: new[] { "FeedId", "OutputId" },
                unique: true);
        }
    }
}
