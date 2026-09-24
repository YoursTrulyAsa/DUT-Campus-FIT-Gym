using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    public partial class LinkWorkoutPlansToExercises : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "WorkoutPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutPlanId",
                table: "WorkoutCompletions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_ExerciseId",
                table: "WorkoutPlans",
                column: "ExerciseId");

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
                name: "FK_WorkoutPlans_Exercises_ExerciseId",
                table: "WorkoutPlans",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                UPDATE wp
                SET ExerciseId = e.ExerciseId
                FROM WorkoutPlans wp
                INNER JOIN Exercises e
                    ON LTRIM(RTRIM(wp.ExerciseName)) = LTRIM(RTRIM(e.ExerciseName))
                WHERE wp.ExerciseId IS NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutCompletions_WorkoutPlans_WorkoutPlanId",
                table: "WorkoutCompletions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_Exercises_ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutCompletions_WorkoutPlanId",
                table: "WorkoutCompletions");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanId",
                table: "WorkoutCompletions");
        }
    }
}
