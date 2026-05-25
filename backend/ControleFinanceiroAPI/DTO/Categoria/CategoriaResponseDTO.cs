namespace ControleFinanceiroAPI.DTO.Categoria
{
    public class CategoriaResponseDTO
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Cor { get; set; }
        public string? UrlIcone { get; set; }
        public string? UsuarioId { get; set; }
    }
}
