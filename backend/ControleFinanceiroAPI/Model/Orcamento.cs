using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Limite de gasto para uma categoria do ambiente, válido até <see cref="DataLimite"/>.
    /// O consumo é calculado em RelatorioService.GetOrcamentosStatusAsync.
    /// </summary>
    public class Orcamento
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string ? Descricao { get; set; }
        public decimal ValorLimite { get; set; }

        /// <summary>
        /// Data final do orçamento. Não existe data de início: o status soma as despesas da categoria
        /// com Data até este limite, desde a primeira transação.
        /// </summary>
        public DateTime DataLimite { get; set; }
        public StatusOrcamento StatusOrcamento { get; set; }
        public Guid CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
    }
}
