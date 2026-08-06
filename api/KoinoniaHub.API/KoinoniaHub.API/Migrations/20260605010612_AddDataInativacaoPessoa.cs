using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoinoniaHub.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDataInativacaoPessoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataInativacao",
                table: "Pessoas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataInativacao",
                table: "Pessoas");
        }
    }
}
