using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class SituacaoAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Plano 6.2: Consolidada = true vira "Consolidada"; o restante vira "EmAberto".
            migrationBuilder.AddColumn<string>(
                name: "Situacao",
                table: "Aulas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "EmAberto");

            migrationBuilder.Sql(
                "UPDATE \"Aulas\" SET \"Situacao\" = 'Consolidada' WHERE \"Consolidada\" = TRUE;");

            migrationBuilder.DropColumn(
                name: "Consolidada",
                table: "Aulas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Consolidada",
                table: "Aulas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Aulas "NaoRealizada" não existem no modelo antigo e voltam como não consolidadas.
            migrationBuilder.Sql(
                "UPDATE \"Aulas\" SET \"Consolidada\" = TRUE WHERE \"Situacao\" = 'Consolidada';");

            migrationBuilder.DropColumn(
                name: "Situacao",
                table: "Aulas");
        }
    }
}