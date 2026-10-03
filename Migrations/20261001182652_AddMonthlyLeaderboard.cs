using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DUT_Campus_FIT_Gym.Migrations
{
    public partial class AddMonthlyLeaderboard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyLeaderboards",
                columns: table => new
                {
                    MonthlyLeaderboardId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Year = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Month = table.Column<int>(
                        type: "int",
                        nullable: false),

                    MemberId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Rank = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Points = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Standing = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false),

                    GiftStatus = table.Column<string>(
                        type: "nvarchar(30)",
                        maxLength: 30,
                        nullable: false),

                    NotificationSent = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    FinalizedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_MonthlyLeaderboards",
                        x => x.MonthlyLeaderboardId);

                    table.ForeignKey(
                        name: "FK_MonthlyLeaderboards_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "MemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyLeaderboards_MemberId",
                table: "MonthlyLeaderboards",
                column: "MemberId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyLeaderboards");
        }
    }
}
