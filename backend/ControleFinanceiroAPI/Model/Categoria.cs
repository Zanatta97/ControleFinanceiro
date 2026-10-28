using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Classificação das transações e dos orçamentos (ex.: Alimentação, Salário). Pertence a um ambiente
    /// e é compartilhada por todos os membros dele.
    /// </summary>
    public class Categoria
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }

        /// <summary>Cor usada nos gráficos e marcadores do frontend (ex.: "#16a34a").</summary>
        public string? Cor { get; set; }

        /// <summary>Ícone da categoria: nome de ícone do Iconify (ex.: "mdi:food") ou URL de imagem.</summary>
        public string? UrlIcone { get; set; }

        /// <summary>Usuário que cadastrou a categoria (registro de autoria; o acesso é controlado pelo ambiente).</summary>
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
    }
}
