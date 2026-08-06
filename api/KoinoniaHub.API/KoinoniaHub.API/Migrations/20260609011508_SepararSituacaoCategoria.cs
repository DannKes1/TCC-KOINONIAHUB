using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class SepararSituacaoCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Cria as colunas novas
            migrationBuilder.AddColumn<string>(
                name: "Situacao",
                table: "Pessoas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Ativo");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Pessoas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Membro");

            // 2) Copia o valor do Status antigo (as 3 colunas coexistem aqui)
            migrationBuilder.Sql(@"UPDATE ""Pessoas"" SET ""Situacao"" = CASE WHEN ""Status"" = 'Inativo' THEN 'Inativo' ELSE 'Ativo' END;");
            migrationBuilder.Sql(@"UPDATE ""Pessoas"" SET ""Categoria"" = CASE WHEN ""Status"" = 'Visitante' THEN 'Visitante' ELSE 'Membro' END;");

            // 3) Só agora remove a coluna antiga
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pessoas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recria o Status e reconstrói o valor antes de remover as colunas novas
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Pessoas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Ativo");

            migrationBuilder.Sql(@"UPDATE ""Pessoas"" SET ""Status"" = CASE WHEN ""Situacao"" = 'Inativo' THEN 'Inativo' WHEN ""Categoria"" = 'Visitante' THEN 'Visitante' ELSE 'Ativo' END;");

            migrationBuilder.DropColumn(
                name: "Situacao",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Pessoas");
        }
    }
}