using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoTFG.Migrations
{
    /// <inheritdoc />
    public partial class Add_Sport_To_Post : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeporteRelacionado",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeporteRelacionado",
                table: "Posts");
        }
    }
}
