using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageHelper.Migrations
{
    /// <inheritdoc />
    public partial class FixTypos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsOredrable",
                table: "Items",
                newName: "IsOrderable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsOrderable",
                table: "Items",
                newName: "IsOredrable");
        }
    }
}
