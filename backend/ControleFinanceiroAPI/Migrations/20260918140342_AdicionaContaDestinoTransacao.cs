using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaContaDestinoTransacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContaDestinoId",
                table: "Transacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_ContaDestinoId",
                table: "Transacoes",
                column: "ContaDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Contas_ContaDestinoId",
                table: "Transacoes",
                column: "ContaDestinoId",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Contas_ContaDestinoId",
                table: "Transacoes");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_ContaDestinoId",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "ContaDestinoId",
                table: "Transacoes");
        }
    }
}
