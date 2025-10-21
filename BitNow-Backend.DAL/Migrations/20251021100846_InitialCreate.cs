using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitNow_Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionHistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    winner_id = table.Column<int>(type: "int", nullable: true),
                    starting_bid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    final_bid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    total_bids = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AuctionH__3213E83F8F90DB51", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__3213E83F277DE0DA", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reputation_score = table.Column<decimal>(type: "decimal(3,2)", nullable: true, defaultValue: 0.00m),
                    total_ratings = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_sales = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_purchases = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3213E83FD002142C", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "pending"),
                    admin_reply = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContactM__3213E83FBD190A4F", x => x.id);
                    table.ForeignKey(
                        name: "FK__ContactMe__user___73BA3083",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "FavoriteSellers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Favorite__3213E83FB7177F4E", x => x.id);
                    table.ForeignKey(
                        name: "FK__FavoriteS__buyer__00200768",
                        column: x => x.buyer_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__FavoriteS__selle__01142BA1",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    category_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    base_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, defaultValue: "pending"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Items__3213E83FB79E5DB3", x => x.id);
                    table.ForeignKey(
                        name: "FK__Items__category___4D94879B",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Items__seller_id__4CA06362",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__3213E83F0684F1AC", x => x.id);
                    table.ForeignKey(
                        name: "FK__Notificat__user___05D8E0BE",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserRole__3213E83F823847CC", x => x.id);
                    table.ForeignKey(
                        name: "FK__UserRoles__user___4316F928",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Auctions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    item_id = table.Column<int>(type: "int", nullable: false),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    starting_bid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    current_bid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    buy_now_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    bid_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    winner_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Auctions__3213E83F5033A638", x => x.id);
                    table.ForeignKey(
                        name: "FK__Auctions__item_i__534D60F1",
                        column: x => x.item_id,
                        principalTable: "Items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Auctions__seller__5441852A",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Auctions__winner__5535A963",
                        column: x => x.winner_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AutoBids",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    max_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AutoBids__3213E83FE492B6C8", x => x.id);
                    table.ForeignKey(
                        name: "FK__AutoBids__auctio__60A75C0F",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__AutoBids__user_i__619B8048",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    bidder_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    bid_time = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())"),
                    is_auto_bid = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Bids__3213E83F55D6A6DC", x => x.id);
                    table.ForeignKey(
                        name: "FK__Bids__auction_id__59FA5E80",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Bids__bidder_id__5AEE82B9",
                        column: x => x.bidder_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sender_id = table.Column<int>(type: "int", nullable: false),
                    receiver_id = table.Column<int>(type: "int", nullable: false),
                    auction_id = table.Column<int>(type: "int", nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_read = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    sent_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Messages__3213E83FCD78A6BD", x => x.id);
                    table.ForeignKey(
                        name: "FK__Messages__auctio__6E01572D",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Messages__receiv__6D0D32F4",
                        column: x => x.receiver_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Messages__sender__6C190EBB",
                        column: x => x.sender_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    rater_id = table.Column<int>(type: "int", nullable: false),
                    rated_id = table.Column<int>(type: "int", nullable: false),
                    rating = table.Column<int>(type: "int", nullable: false),
                    comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ratings__3213E83F00FD3E6D", x => x.id);
                    table.ForeignKey(
                        name: "FK__Ratings__auction__797309D9",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Ratings__rated_i__7B5B524B",
                        column: x => x.rated_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Ratings__rater_i__7A672E12",
                        column: x => x.rater_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Watchlist",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    added_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Watchlis__3213E83F49C30639", x => x.id);
                    table.ForeignKey(
                        name: "FK__Watchlist__aucti__6754599E",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Watchlist__user___66603565",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_history_seller",
                table: "AuctionHistory",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_history_winner",
                table: "AuctionHistory",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_status",
                table: "Auctions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_item_id",
                table: "Auctions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_seller_id",
                table: "Auctions",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_winner_id",
                table: "Auctions",
                column: "winner_id");

            migrationBuilder.CreateIndex(
                name: "IX_AutoBids_user_id",
                table: "AutoBids",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__AutoBids__C46C653189F6A89F",
                table: "AutoBids",
                columns: new[] { "auction_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_bids_auction",
                table: "Bids",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_bids_bidder",
                table: "Bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Categori__32DD1E4CD9936ABD",
                table: "Categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_user_id",
                table: "ContactMessages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_favorites_buyer",
                table: "FavoriteSellers",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteSellers_seller_id",
                table: "FavoriteSellers",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Favorite__DD51D1FAAF26F0F0",
                table: "FavoriteSellers",
                columns: new[] { "buyer_id", "seller_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_items_category",
                table: "Items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_seller",
                table: "Items",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_receiver",
                table: "Messages",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                table: "Messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_auction_id",
                table: "Messages",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user",
                table: "Notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_ratings_rated",
                table: "Ratings",
                column: "rated_id");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_rater_id",
                table: "Ratings",
                column: "rater_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Ratings__6384321E0C855D8C",
                table: "Ratings",
                columns: new[] { "auction_id", "rater_id", "rated_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_roles_user",
                table: "UserRoles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__UserRole__31DDE51AC6C34E00",
                table: "UserRoles",
                columns: new[] { "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "Users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E6164AD34E528",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlist_auction_id",
                table: "Watchlist",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Watchlis__AB414F6AC4A841B0",
                table: "Watchlist",
                columns: new[] { "user_id", "auction_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionHistory");

            migrationBuilder.DropTable(
                name: "AutoBids");

            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.DropTable(
                name: "FavoriteSellers");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Watchlist");

            migrationBuilder.DropTable(
                name: "Auctions");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
