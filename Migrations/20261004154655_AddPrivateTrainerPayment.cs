using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateTrainerPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrivateTrainerSubscriptionId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrivateTrainerSubscriptions",
                columns: table => new
                {
                    PrivateTrainerSubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateTrainerSubscriptions", x => x.PrivateTrainerSubscriptionId);
                    table.ForeignKey(
                        name: "FK_PrivateTrainerSubscriptions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrivateTrainerSubscriptions_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "TrainerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PrivateTrainerSubscriptionId",
                table: "Payments",
                column: "PrivateTrainerSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateTrainerSubscriptions_MemberId",
                table: "PrivateTrainerSubscriptions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateTrainerSubscriptions_TrainerId",
                table: "PrivateTrainerSubscriptions",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PrivateTrainerSubscriptions_PrivateTrainerSubscriptionId",
                table: "Payments",
                column: "PrivateTrainerSubscriptionId",
                principalTable: "PrivateTrainerSubscriptions",
                principalColumn: "PrivateTrainerSubscriptionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PrivateTrainerSubscriptions_PrivateTrainerSubscriptionId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "PrivateTrainerSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PrivateTrainerSubscriptionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PrivateTrainerSubscriptionId",
                table: "Payments");
        }
    }
}
