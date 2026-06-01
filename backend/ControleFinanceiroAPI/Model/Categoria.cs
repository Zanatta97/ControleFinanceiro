using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    public class Categoria
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Cor { get; set; }
        public string? UrlIcone { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
    }
}
