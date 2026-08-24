using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndRenewalToMembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FirstTimeMember",
                table: "Memberships",
                newName: "IsRenewal");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Memberships",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MembershipType",
                table: "Memberships",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Memberships",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "MembershipApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "MembershipApplications");

            migrationBuilder.RenameColumn(
                name: "IsRenewal",
                table: "Memberships",
                newName: "FirstTimeMember");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Memberships",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "MembershipType",
                table: "Memberships",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
