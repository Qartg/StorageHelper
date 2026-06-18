using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageHelper.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOrderable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOredrable",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOredrable",
                table: "Items");
        }
    }
}
