using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainerPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Trainers");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "Trainers",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "MemberId",
                table: "Trainers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "Trainers");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Trainers",
                newName: "FullName");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Trainers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
