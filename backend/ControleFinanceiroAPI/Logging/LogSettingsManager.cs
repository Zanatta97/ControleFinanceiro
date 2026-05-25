namespace ControleFinanceiroAPI.Logging
{
    // Singleton responsável por guardar o estado atual do log em memória.
    // Por ser Singleton, existe uma única instância compartilhada entre todos os requests —
    // ou seja, quando o estado muda, todos os requests seguintes já enxergam o novo valor.
    public class LogSettingsManager
    {
        // volatile garante que, em cenários com múltiplos requests simultâneos (threads diferentes),
        // cada leitura busque o valor direto da memória principal, sem usar cache de CPU.
        // Para um bool simples, isso é suficiente para garantir consistência entre threads.
        private volatile bool _logAllRequests = false;

        public bool LogAllRequests => _logAllRequests;

        public void Ativar() => _logAllRequests = true;

        public void Desativar() => _logAllRequests = false;
    }
}
