namespace ControleFinanceiroAPI.Services
{
    /// <summary>
    /// Mantém em memória a data do último reset do ambiente de demonstração.
    /// Registrado como Singleton (mesma ideia do LogSettingsManager): uma única
    /// instância compartilhada entre as requisições, sem precisar de tabela/coluna nova.
    ///
    /// Em um cold start do App Service o estado volta a null, fazendo o próximo
    /// acesso demo limpar e recriar os dados — comportamento desejado (mantém o
    /// ambiente sempre enxuto).
    /// </summary>
    public class DemoStateManager
    {
        private readonly object _lock = new();
        private DateOnly? _ultimoResetUtc;

        /// <summary>
        /// Retorna true (e marca a data de hoje) se ainda não houve reset hoje.
        /// A verificação e a marcação são atômicas para evitar resets concorrentes.
        /// </summary>
        public bool PrecisaResetar()
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            lock (_lock)
            {
                if (_ultimoResetUtc == hoje)
                    return false;

                _ultimoResetUtc = hoje;
                return true;
            }
        }
    }
}
