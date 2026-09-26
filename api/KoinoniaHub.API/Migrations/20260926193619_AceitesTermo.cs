using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class AceitesTermo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AceitesTermo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    IgrejaId = table.Column<int>(type: "integer", nullable: false),
                    TermoVersao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TermoHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AceitoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Meio = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AceitesTermo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AceitesTermo_Igrejas_IgrejaId",
                        column: x => x.IgrejaId,
                        principalTable: "Igrejas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AceitesTermo_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AceitesTermo_IgrejaId",
                table: "AceitesTermo",
                column: "IgrejaId");

            migrationBuilder.CreateIndex(
                name: "IX_AceitesTermo_UsuarioId",
                table: "AceitesTermo",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AceitesTermo");
        }
    }
}
