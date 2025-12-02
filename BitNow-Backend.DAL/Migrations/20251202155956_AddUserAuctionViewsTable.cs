using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitNow_Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuctionViewsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAuctionViews",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    viewed_at = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserAuct__3213E83F", x => x.id);
                    table.ForeignKey(
                        name: "FK__UserAucti__aucti__00200768",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__UserAucti__user___7F2BE32F",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_auction_views_user_auction",
                table: "UserAuctionViews",
                columns: new[] { "user_id", "auction_id" });

            migrationBuilder.CreateIndex(
                name: "idx_user_auction_views_viewed_at",
                table: "UserAuctionViews",
                column: "viewed_at");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuctionViews_auction_id",
                table: "UserAuctionViews",
                column: "auction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAuctionViews");
        }
    }
}
