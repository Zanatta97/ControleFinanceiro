namespace ControleFinanceiroAPI.DTO.Ambiente
{
    public class AmbienteResponseDTO
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public DateTime? DataCriacao { get; set; }
        public List<AmbienteMembroResponseDTO> Membros { get; set; } = [];
    }
}
