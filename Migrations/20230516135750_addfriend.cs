using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatOnWebApi.Migrations
{
    /// <inheritdoc />
    public partial class addfriend : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.RenameColumn(
                name: "ToUserId",
                table: "Friends",
                newName: "ToUserIdId");

            migrationBuilder.RenameColumn(
                name: "FromUserId",
                table: "Friends",
                newName: "FromUserIdId");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_FromUserIdId",
                table: "Friends",
                column: "FromUserIdId");

            migrationBuilder.CreateIndex(
                name: "IX_Friends_ToUserIdId",
                table: "Friends",
                column: "ToUserIdId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_Users_FromUserIdId",
                table: "Friends",
                column: "FromUserIdId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_Users_ToUserIdId",
                table: "Friends",
                column: "ToUserIdId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friends_Users_FromUserIdId",
                table: "Friends");

            migrationBuilder.DropForeignKey(
                name: "FK_Friends_Users_ToUserIdId",
                table: "Friends");

            migrationBuilder.DropIndex(
                name: "IX_Friends_FromUserIdId",
                table: "Friends");

            migrationBuilder.DropIndex(
                name: "IX_Friends_ToUserIdId",
                table: "Friends");

            migrationBuilder.RenameColumn(
                name: "ToUserIdId",
                table: "Friends",
                newName: "ToUserId");

            migrationBuilder.RenameColumn(
                name: "FromUserIdId",
                table: "Friends",
                newName: "FromUserId");

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reciever = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                });
        }
    }
}
