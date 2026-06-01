using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    public class Orcamento
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string ? Descricao { get; set; }
        public decimal ValorLimite { get; set; }
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
