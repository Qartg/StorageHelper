using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageHelper.Migrations
{
    /// <inheritdoc />
    public partial class AddItemFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vendor",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Sku",
                table: "Items",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_Sku",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "Items");
        }
    }
}
