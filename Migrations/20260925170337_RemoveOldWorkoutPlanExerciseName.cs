using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldWorkoutPlanExerciseName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutCompletions_WorkoutPlans_WorkoutPlanId",
                table: "WorkoutCompletions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutCompletions_WorkoutPlanId",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanId",
                table: "WorkoutCompletions");

            migrationBuilder.AddColumn<string>(
                name: "FitnessLevel",
                table: "WorkoutCompletions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "WorkoutCompletions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorkoutDay",
                table: "WorkoutCompletions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId_WeekNumber_WorkoutDay",
                table: "WorkoutCompletions",
                columns: new[] { "WorkoutProgrammeId", "WeekNumber", "WorkoutDay" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutCompletions_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutCompletions",
                column: "WorkoutProgrammeId",
                principalTable: "WorkoutProgrammes",
                principalColumn: "WorkoutProgrammeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans",
                column: "WorkoutProgrammeId",
                principalTable: "WorkoutProgrammes",
                principalColumn: "WorkoutProgrammeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutCompletions_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutCompletions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutCompletions_WorkoutProgrammeId_WeekNumber_WorkoutDay",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "FitnessLevel",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WorkoutDay",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "WorkoutProgrammeId",
                table: "WorkoutCompletions");

            migrationBuilder.AddColumn<int>(
                name: "WorkoutPlanId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutCompletions_WorkoutPlanId",
                table: "WorkoutCompletions",
                column: "WorkoutPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutCompletions_WorkoutPlans_WorkoutPlanId",
                table: "WorkoutCompletions",
                column: "WorkoutPlanId",
                principalTable: "WorkoutPlans",
                principalColumn: "WorkoutPlanId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans",
                column: "WorkoutProgrammeId",
                principalTable: "WorkoutProgrammes",
                principalColumn: "WorkoutProgrammeId");
        }
    }
}
