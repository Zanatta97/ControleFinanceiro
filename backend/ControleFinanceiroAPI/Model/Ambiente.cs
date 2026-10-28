namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Espaço financeiro compartilhável (ex.: pessoal, família, empresa). Contas, categorias,
    /// transações, orçamentos e saldos mensais pertencem a um ambiente, e o usuário só enxerga
    /// os dados do ambiente selecionado no token (claim "ambiente_id").
    /// </summary>
    public class Ambiente
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }

        /// <summary>
        /// Usuário que criou o ambiente. Não muda quando a posse é transferida: o Dono atual
        /// é o membro com Role "Dono" em <see cref="Membros"/>.
        /// </summary>
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public DateTime? DataCriacao { get; set; } = DateTime.Now;

        /// <summary>Usuários com acesso ao ambiente e o papel de cada um ("Dono", "Admin" ou "Membro").</summary>
        public List<AmbienteMembro> Membros { get; set; } = [];
    }
}
