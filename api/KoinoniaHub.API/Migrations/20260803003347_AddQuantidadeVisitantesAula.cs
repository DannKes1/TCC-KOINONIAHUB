using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantidadeVisitantesAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantidadeVisitantes",
                table: "Aulas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantidadeVisitantes",
                table: "Aulas");
        }
    }
}
