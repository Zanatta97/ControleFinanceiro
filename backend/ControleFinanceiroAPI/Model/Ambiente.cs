namespace ControleFinanceiroAPI.Model
{
    public class Ambiente
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
