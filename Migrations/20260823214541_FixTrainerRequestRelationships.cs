using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class FixTrainerRequestRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainerRequests_Members_TrainerId",
                table: "TrainerRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerRequests_Trainers_TrainerId",
                table: "TrainerRequests",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "TrainerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainerRequests_Trainers_TrainerId",
                table: "TrainerRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainerRequests_Members_TrainerId",
                table: "TrainerRequests",
                column: "TrainerId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
