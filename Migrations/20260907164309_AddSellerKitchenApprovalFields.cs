using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cloud_Kitchen_web_App.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerKitchenApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "KitchenOwners");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MenuItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Kitchens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Kitchens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Kitchens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Kitchens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClosingTime",
                table: "Kitchens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cuisine",
                table: "Kitchens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAreas",
                table: "Kitchens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryRatePerKm",
                table: "Kitchens",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "OpeningTime",
                table: "Kitchens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Kitchens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Kitchens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "ClosingTime",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "Cuisine",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "DeliveryAreas",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "DeliveryRatePerKm",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "OpeningTime",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Kitchens");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Kitchens");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MenuItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "KitchenOwners",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
