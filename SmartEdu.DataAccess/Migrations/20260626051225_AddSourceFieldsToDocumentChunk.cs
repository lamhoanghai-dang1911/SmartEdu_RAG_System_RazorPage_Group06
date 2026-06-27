using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartEdu.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceFieldsToDocumentChunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "DocumentChunks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionTitle",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SectionTitle",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "DocumentChunks");
        }
    }
}
