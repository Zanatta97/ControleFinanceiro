using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    public class Conta
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public TipoConta TipoConta { get; set; }
        public decimal Saldo { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
