using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class RenameMemberFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StaffStudentNumber",
                table: "Members",
                newName: "StudentNumber");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Members",
                newName: "Surname");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Members",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Surname",
                table: "Members",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "StudentNumber",
                table: "Members",
                newName: "StaffStudentNumber");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Members",
                newName: "FirstName");
        }
    }
}
