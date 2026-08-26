using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipApplicationsAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembershipApplications",
                columns: table => new
                {
                    MembershipApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    MembershipType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerificationDocument = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminComment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipApplications", x => x.MembershipApplicationId);
                    table.ForeignKey(
                        name: "FK_MembershipApplications_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<int>(type: "int", nullable: false),
                    MembershipId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemberId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Members_MemberId1",
                        column: x => x.MemberId1,
                        principalTable: "Members",
                        principalColumn: "MemberId");
                    table.ForeignKey(
                        name: "FK_Payments_Memberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "Memberships",
                        principalColumn: "MembershipId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MembershipApplications_MemberId",
                table: "MembershipApplications",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberId",
                table: "Payments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberId1",
                table: "Payments",
                column: "MemberId1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MembershipId",
                table: "Payments",
                column: "MembershipId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembershipApplications");

            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
