using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Points",
                table: "Members");
        }
    }
}
