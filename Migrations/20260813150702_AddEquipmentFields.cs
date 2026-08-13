using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "Equipment",
                newName: "EquipmentID");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Equipment",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Equipment",
                newName: "EquipmentName");

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Equipment",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "EquipmentID",
                table: "Equipment",
                newName: "EquipmentId");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Equipment",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "EquipmentName",
                table: "Equipment",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
