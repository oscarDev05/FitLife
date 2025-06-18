using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoTFG.Migrations
{
    /// <inheritdoc />
    public partial class Migration_user_image : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Foto_perfil_bytes",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Foto_perfil",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Foto_perfil",
                table: "Users");

            migrationBuilder.AddColumn<byte[]>(
                name: "Foto_perfil_bytes",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
