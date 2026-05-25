namespace ControleFinanceiroAPI.Logging
{
    // Classe que representa a seção "LogSettings" do appsettings.json.
    // O ASP.NET Core faz o "bind" automático: cada propriedade aqui corresponde
    // a uma chave no JSON, mantendo os valores sincronizados.
    public class LogSettings
    {
        // Nome da seção no appsettings.json. Usado no Program.cs para localizar as configurações.
        public const string SectionName = "LogSettings";

        // Quando false (padrão): só loga requests que resultaram em erro (StatusCode >= 400).
        // Quando true: loga absolutamente todos os requests e respostas.
        // Alterar esse valor no appsettings.json tem efeito imediato, sem precisar reiniciar a API.
        public bool LogAllRequests { get; set; } = false;
    }
}
