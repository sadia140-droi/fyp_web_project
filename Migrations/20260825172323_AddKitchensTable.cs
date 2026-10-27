using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud_Kitchen_web_App.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kitchen_KitchenOwner_KitchenOwnerId",
                table: "Kitchen");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Kitchen_KitchenId",
                table: "MenuItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Kitchen",
                table: "Kitchen");

            migrationBuilder.RenameTable(
                name: "Kitchen",
                newName: "Kitchens");

            migrationBuilder.RenameIndex(
                name: "IX_Kitchen_KitchenOwnerId",
                table: "Kitchens",
                newName: "IX_Kitchens_KitchenOwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Kitchens",
                table: "Kitchens",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kitchens_KitchenOwner_KitchenOwnerId",
                table: "Kitchens",
                column: "KitchenOwnerId",
                principalTable: "KitchenOwner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Kitchens_KitchenId",
                table: "MenuItems",
                column: "KitchenId",
                principalTable: "Kitchens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kitchens_KitchenOwner_KitchenOwnerId",
                table: "Kitchens");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_Kitchens_KitchenId",
                table: "MenuItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Kitchens",
                table: "Kitchens");

            migrationBuilder.RenameTable(
                name: "Kitchens",
                newName: "Kitchen");

            migrationBuilder.RenameIndex(
                name: "IX_Kitchens_KitchenOwnerId",
                table: "Kitchen",
                newName: "IX_Kitchen_KitchenOwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Kitchen",
                table: "Kitchen",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Kitchen_KitchenOwner_KitchenOwnerId",
                table: "Kitchen",
                column: "KitchenOwnerId",
                principalTable: "KitchenOwner",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_Kitchen_KitchenId",
                table: "MenuItems",
                column: "KitchenId",
                principalTable: "Kitchen",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
