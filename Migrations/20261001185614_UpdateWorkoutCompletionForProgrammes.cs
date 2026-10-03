using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWorkoutCompletionForProgrammes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "WorkoutCompletions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "WorkoutDay",
                table: "WorkoutCompletions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FitnessLevel",
                table: "WorkoutCompletions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Beginner");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId",
                table: "WorkoutCompletions",
                column: "WorkoutProgrammeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutCompletions_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutCompletions",
                column: "WorkoutProgrammeId",
                principalTable: "WorkoutProgrammes",
                principalColumn: "WorkoutProgrammeId",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutCompletions_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutCompletions");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WorkoutDay",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "FitnessLevel",
                table: "WorkoutCompletions");
        }
    }
}
