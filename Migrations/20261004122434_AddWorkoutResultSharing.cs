using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutResultSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutResults",
                columns: table => new
                {
                    WorkoutResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    WorkoutProgrammeId = table.Column<int>(type: "int", nullable: false),
                    BeforePhoto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AfterPhoto = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutResults", x => x.WorkoutResultId);
                    table.ForeignKey(
                        name: "FK_WorkoutResults_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkoutResults_WorkoutProgrammes_WorkoutProgrammeId",
                        column: x => x.WorkoutProgrammeId,
                        principalTable: "WorkoutProgrammes",
                        principalColumn: "WorkoutProgrammeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedWorkoutResults",
                columns: table => new
                {
                    SavedWorkoutResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    WorkoutResultId = table.Column<int>(type: "int", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedWorkoutResults", x => x.SavedWorkoutResultId);
                    table.ForeignKey(
                        name: "FK_SavedWorkoutResults_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedWorkoutResults_WorkoutResults_WorkoutResultId",
                        column: x => x.WorkoutResultId,
                        principalTable: "WorkoutResults",
                        principalColumn: "WorkoutResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedWorkoutResults_MemberId_WorkoutResultId",
                table: "SavedWorkoutResults",
                columns: new[] { "MemberId", "WorkoutResultId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedWorkoutResults_WorkoutResultId",
                table: "SavedWorkoutResults",
                column: "WorkoutResultId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutResults_MemberId",
                table: "WorkoutResults",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutResults_WorkoutProgrammeId",
                table: "WorkoutResults",
                column: "WorkoutProgrammeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedWorkoutResults");

            migrationBuilder.DropTable(
                name: "WorkoutResults");
        }
    }
}
