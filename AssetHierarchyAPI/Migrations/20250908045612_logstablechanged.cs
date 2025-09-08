using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetHierarchyAPI.Migrations
{
    /// <inheritdoc />
    public partial class logstablechanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Assetslogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Assetslogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
