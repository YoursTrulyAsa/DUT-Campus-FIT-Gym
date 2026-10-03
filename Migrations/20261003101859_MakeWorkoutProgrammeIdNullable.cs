using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class MakeWorkoutProgrammeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId_WeekNumber_WorkoutDay",
                table: "WorkoutCompletions",
                columns: new[] { "WorkoutProgrammeId", "WeekNumber", "WorkoutDay" },
                unique: true,
                filter: "[WorkoutProgrammeId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId_WeekNumber_WorkoutDay",
                table: "WorkoutCompletions");

            migrationBuilder.AlterColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId_WeekNumber_WorkoutDay",
                table: "WorkoutCompletions",
                columns: new[] { "WorkoutProgrammeId", "WeekNumber", "WorkoutDay" },
                unique: true);
        }
    }
}
