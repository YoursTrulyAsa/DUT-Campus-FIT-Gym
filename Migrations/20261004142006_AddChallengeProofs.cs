using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeProofs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeProofs",
                columns: table => new
                {
                    ChallengeProofId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FitnessChallengeId = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    VideoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrainerComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByTrainerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeProofs", x => x.ChallengeProofId);
                    table.ForeignKey(
                        name: "FK_ChallengeProofs_FitnessChallenges_FitnessChallengeId",
                        column: x => x.FitnessChallengeId,
                        principalTable: "FitnessChallenges",
                        principalColumn: "FitnessChallengeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeProofs_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChallengeProofs_Trainers_ReviewedByTrainerId",
                        column: x => x.ReviewedByTrainerId,
                        principalTable: "Trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProofs_FitnessChallengeId",
                table: "ChallengeProofs",
                column: "FitnessChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProofs_MemberId",
                table: "ChallengeProofs",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeProofs_ReviewedByTrainerId",
                table: "ChallengeProofs",
                column: "ReviewedByTrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeProofs");
        }
    }
}
