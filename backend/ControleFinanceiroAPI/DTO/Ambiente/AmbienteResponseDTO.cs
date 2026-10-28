namespace ControleFinanceiroAPI.DTO.Ambiente
{
    /// <summary>Ambiente com a lista de membros; o frontend deriva dela o papel do usuário logado.</summary>
    public class AmbienteResponseDTO
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public DateTime? DataCriacao { get; set; }
        public List<AmbienteMembroResponseDTO> Membros { get; set; } = [];
    }
}
