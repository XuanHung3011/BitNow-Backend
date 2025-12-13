using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitNow_Backend.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeIdToMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisputeId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_DisputeId",
                table: "Messages",
                column: "DisputeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Disputes_DisputeId",
                table: "Messages",
                column: "DisputeId",
                principalTable: "Disputes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Disputes_DisputeId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_DisputeId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "DisputeId",
                table: "Messages");
        }
    }
}
