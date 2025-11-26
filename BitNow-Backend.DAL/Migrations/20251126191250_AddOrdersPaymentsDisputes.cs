using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitNow_Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersPaymentsDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    auction_id = table.Column<int>(type: "int", nullable: false),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    final_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    order_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    cancel_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tracking_number = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    shipping_company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    shipped_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    shipping_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Orders__3213E83F", x => x.id);
                    table.ForeignKey(
                        name: "FK__Orders__auction__6A30C649",
                        column: x => x.auction_id,
                        principalTable: "Auctions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Orders__buyer_id__6B24EA82",
                        column: x => x.buyer_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Orders__seller_i__6C190EBB",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Disputes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    buyer_id = table.Column<int>(type: "int", nullable: false),
                    seller_id = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    resolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    resolved_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    resolved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    admin_notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Disputes__3213E83F", x => x.id);
                    table.ForeignKey(
                        name: "FK__Disputes__buyer___6FE99F9F",
                        column: x => x.buyer_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Disputes__order__6EF57B66",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Disputes__resolv__71D1E811",
                        column: x => x.resolved_by,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Disputes__selle__70DDC3D8",
                        column: x => x.seller_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    payment_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transaction_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    payment_provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    released_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payments__3213E83F", x => x.id);
                    table.ForeignKey(
                        name: "FK__Payments__order___6D0D32F4",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_disputes_status",
                table: "Disputes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_buyer_id",
                table: "Disputes",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_resolved_by",
                table: "Disputes",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_Disputes_seller_id",
                table: "Disputes",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "UQ__Disputes__order_id",
                table: "Disputes",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_auction",
                table: "Orders",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_buyer",
                table: "Orders",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_seller",
                table: "Orders",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "Orders",
                column: "order_status");

            migrationBuilder.CreateIndex(
                name: "idx_payments_status",
                table: "Payments",
                column: "payment_status");

            migrationBuilder.CreateIndex(
                name: "UQ__Payments__order_id",
                table: "Payments",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Disputes");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
