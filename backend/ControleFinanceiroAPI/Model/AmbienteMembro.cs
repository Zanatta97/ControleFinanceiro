namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Vínculo entre um usuário e um ambiente. A chave é composta (AmbienteId + UsuarioId),
    /// então um usuário aparece no máximo uma vez por ambiente.
    /// </summary>
    public class AmbienteMembro
    {
        public Guid AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
        public string UsuarioId { get; set; } = null!;
        public Usuario? Usuario { get; set; }

        /// <summary>
        /// Papel do usuário dentro do ambiente — não confundir com as roles do Identity (Admin, User, Demo),
        /// que valem para o sistema todo. O AmbienteService grava "Dono" (criador ou quem recebeu a posse),
        /// "Admin" (Dono anterior, rebaixado na transferência) ou "Membro" (adicionado por e-mail).
        /// O valor padrão "User" não é gravado por nenhum fluxo atual.
        /// </summary>
        public string Role { get; set; } = "User";
    }
}
