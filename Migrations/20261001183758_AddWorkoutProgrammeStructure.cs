using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    public partial class AddWorkoutProgrammeStructure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // 1. Create WorkoutProgrammes table
            // ============================================================
            migrationBuilder.CreateTable(
                name: "WorkoutProgrammes",
                columns: table => new
                {
                    WorkoutProgrammeId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    MemberId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ProgrammeName = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    FitnessLevel = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false),

                    Goal = table.Column<string>(
                        type: "nvarchar(100)",
                        maxLength: 100,
                        nullable: false),

                    StartDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false),

                    EndDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false),

                    IsCompleted = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_WorkoutProgrammes",
                        x => x.WorkoutProgrammeId);

                    table.ForeignKey(
                        name: "FK_WorkoutProgrammes_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgrammes_MemberId",
                table: "WorkoutProgrammes",
                column: "MemberId");


            // ============================================================
            // 2. Add WorkoutProgrammeId to existing WorkoutPlans
            // ============================================================
            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutPlans",
                type: "int",
                nullable: true);


            // ============================================================
            // 3. Add ExerciseId to existing WorkoutPlans
            // ============================================================
            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "WorkoutPlans",
                type: "int",
                nullable: true);


            // ============================================================
            // 4. Add WeekNumber to existing WorkoutPlans
            // Existing plans will start at Week 1.
            // ============================================================
            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "WorkoutPlans",
                type: "int",
                nullable: false,
                defaultValue: 1);


            // ============================================================
            // 5. Add FitnessLevel to existing WorkoutProfiles
            // Existing profiles will start at Beginner.
            // ============================================================
            migrationBuilder.AddColumn<string>(
                name: "FitnessLevel",
                table: "WorkoutProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Beginner");


            // ============================================================
            // 6. Index WorkoutPlans foreign keys
            // ============================================================
            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_ExerciseId",
                table: "WorkoutPlans",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_WorkoutProgrammeId",
                table: "WorkoutPlans",
                column: "WorkoutProgrammeId");


            // ============================================================
            // 7. Foreign key: WorkoutPlans -> Exercises
            // ============================================================
            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_Exercises_ExerciseId",
                table: "WorkoutPlans",
                column: "ExerciseId",
                principalTable: "Exercises",
                principalColumn: "ExerciseId",
                onDelete: ReferentialAction.Restrict);


            // ============================================================
            // 8. Foreign key: WorkoutPlans -> WorkoutProgrammes
            // ============================================================
            migrationBuilder.AddForeignKey(
    name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
    table: "WorkoutPlans",
    column: "WorkoutProgrammeId",
    principalTable: "WorkoutProgrammes",
    principalColumn: "WorkoutProgrammeId",
    onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_Exercises_ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "FitnessLevel",
                table: "WorkoutProfiles");

            migrationBuilder.DropTable(
                name: "WorkoutProgrammes");
        }
    }
}