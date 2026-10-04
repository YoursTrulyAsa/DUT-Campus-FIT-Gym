using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentPenaltyPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MembershipId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentPenaltyId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EquipmentPenaltyId",
                table: "Payments",
                column: "EquipmentPenaltyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_EquipmentPenalties_EquipmentPenaltyId",
                table: "Payments",
                column: "EquipmentPenaltyId",
                principalTable: "EquipmentPenalties",
                principalColumn: "EquipmentPenaltyId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_EquipmentPenalties_EquipmentPenaltyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EquipmentPenaltyId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EquipmentPenaltyId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "MembershipId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
