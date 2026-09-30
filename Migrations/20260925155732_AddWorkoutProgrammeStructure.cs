using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutProgrammeStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeekNumber",
                table: "WorkoutPlans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutProgrammeId",
                table: "WorkoutPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkoutProgrammes",
                columns: table => new
                {
                    WorkoutProgrammeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    ProgrammeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FitnessLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutProgrammes", x => x.WorkoutProgrammeId);
                    table.ForeignKey(
                        name: "FK_WorkoutProgrammes_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_WorkoutProgrammeId",
                table: "WorkoutPlans",
                column: "WorkoutProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutProgrammes_MemberId",
                table: "WorkoutProgrammes",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans",
                column: "WorkoutProgrammeId",
                principalTable: "WorkoutProgrammes",
                principalColumn: "WorkoutProgrammeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_WorkoutProgrammes_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropTable(
                name: "WorkoutProgrammes");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_WorkoutProgrammeId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WeekNumber",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WorkoutProgrammeId",
                table: "WorkoutPlans");
        }
    }
}
