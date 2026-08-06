using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class IndiceUnicoMatriculaAtiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlunosDepartamentos_DepartamentoId",
                table: "AlunosDepartamentos");

            migrationBuilder.CreateIndex(
                name: "IX_AlunosDepartamentos_DepartamentoId_PessoaId",
                table: "AlunosDepartamentos",
                columns: new[] { "DepartamentoId", "PessoaId" },
                unique: true,
                filter: "\"Ativo\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlunosDepartamentos_DepartamentoId_PessoaId",
                table: "AlunosDepartamentos");

            migrationBuilder.CreateIndex(
                name: "IX_AlunosDepartamentos_DepartamentoId",
                table: "AlunosDepartamentos",
                column: "DepartamentoId");
        }
    }
}
