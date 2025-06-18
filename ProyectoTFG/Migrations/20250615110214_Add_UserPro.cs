using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoTFG.Migrations
{
    /// <inheritdoc />
    public partial class Add_UserPro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Users_ReceiverId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Users_SenderId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId1",
                table: "Conversations");

            migrationBuilder.AddColumn<bool>(
                name: "IsPro",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnuncio",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId1_UserId2",
                table: "Conversations",
                columns: new[] { "UserId1", "UserId2" },
                unique: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Users_ReceiverId",
                table: "Solicitudes",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Users_SenderId",
                table: "Solicitudes",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Users_ReceiverId",
                table: "Solicitudes");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Users_SenderId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId1_UserId2",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "IsPro",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnuncio",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId1",
                table: "Conversations",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Users_ReceiverId",
                table: "Solicitudes",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Users_SenderId",
                table: "Solicitudes",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
