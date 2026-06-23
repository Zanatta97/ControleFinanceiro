using ControleFinanceiroAPI.DTO.Common;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IDemoService
    {
        /// <summary>
        /// Faz o login do usuário de demonstração (credenciais no servidor),
        /// limpa e recria os dados do ambiente demo caso seja um novo dia,
        /// e retorna um token já com o ambiente demo selecionado.
        /// </summary>
        Task<ApiResponseDTO<object>> LoginDemoAsync();
    }
}
