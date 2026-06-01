namespace ControleFinanceiroAPI.Model
{
    public class AmbienteMembro
    {
        public Guid AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
        public string UsuarioId { get; set; } = null!;
        public Usuario? Usuario { get; set; }
        public string Role { get; set; } = "User";
    }
}
