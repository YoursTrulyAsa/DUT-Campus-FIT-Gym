using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengePoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsAwarded",
                table: "FitnessChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "FitnessChallenges");
        }
    }
}
