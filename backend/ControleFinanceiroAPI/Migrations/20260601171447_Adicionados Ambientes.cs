using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiroAPI.Migrations
{
    /// <inheritdoc />
    public partial class AdicionadosAmbientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteId",
                table: "Transacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteId",
                table: "Orcamentos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteId",
                table: "Contas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteId",
                table: "Categorias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AmbienteAtivoId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ambientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ambientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ambientes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AmbienteMembros",
                columns: table => new
                {
                    AmbienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmbienteMembros", x => new { x.AmbienteId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_AmbienteMembros_Ambientes_AmbienteId",
                        column: x => x.AmbienteId,
                        principalTable: "Ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AmbienteMembros_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacoes_AmbienteId",
                table: "Transacoes",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Orcamentos_AmbienteId",
                table: "Orcamentos",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Contas_AmbienteId",
                table: "Contas",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_AmbienteId",
                table: "Categorias",
                column: "AmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AmbienteAtivoId",
                table: "AspNetUsers",
                column: "AmbienteAtivoId");

            migrationBuilder.CreateIndex(
                name: "IX_AmbienteMembros_UsuarioId",
                table: "AmbienteMembros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ambientes_UsuarioId",
                table: "Ambientes",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Ambientes_AmbienteAtivoId",
                table: "AspNetUsers",
                column: "AmbienteAtivoId",
                principalTable: "Ambientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Ambientes_AmbienteId",
                table: "Categorias",
                column: "AmbienteId",
                principalTable: "Ambientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Contas_Ambientes_AmbienteId",
                table: "Contas",
                column: "AmbienteId",
                principalTable: "Ambientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orcamentos_Ambientes_AmbienteId",
                table: "Orcamentos",
                column: "AmbienteId",
                principalTable: "Ambientes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transacoes_Ambientes_AmbienteId",
                table: "Transacoes",
                column: "AmbienteId",
                principalTable: "Ambientes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Ambientes_AmbienteAtivoId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Ambientes_AmbienteId",
                table: "Categorias");

            migrationBuilder.DropForeignKey(
                name: "FK_Contas_Ambientes_AmbienteId",
                table: "Contas");

            migrationBuilder.DropForeignKey(
                name: "FK_Orcamentos_Ambientes_AmbienteId",
                table: "Orcamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Transacoes_Ambientes_AmbienteId",
                table: "Transacoes");

            migrationBuilder.DropTable(
                name: "AmbienteMembros");

            migrationBuilder.DropTable(
                name: "Ambientes");

            migrationBuilder.DropIndex(
                name: "IX_Transacoes_AmbienteId",
                table: "Transacoes");

            migrationBuilder.DropIndex(
                name: "IX_Orcamentos_AmbienteId",
                table: "Orcamentos");

            migrationBuilder.DropIndex(
                name: "IX_Contas_AmbienteId",
                table: "Contas");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_AmbienteId",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AmbienteAtivoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AmbienteId",
                table: "Transacoes");

            migrationBuilder.DropColumn(
                name: "AmbienteId",
                table: "Orcamentos");

            migrationBuilder.DropColumn(
                name: "AmbienteId",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "AmbienteId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "AmbienteAtivoId",
                table: "AspNetUsers");
        }
    }
}
