using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Services
{
    public class DemoService : IDemoService
    {
        private readonly ILogger<DemoService> _logger;
        private readonly AppDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly DemoStateManager _demoState;

        public DemoService(ILogger<DemoService> logger,
                           AppDbContext db,
                           UserManager<Usuario> userManager,
                           ITokenService tokenService,
                           IConfiguration configuration,
                           DemoStateManager demoState)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
            _tokenService = tokenService;
            _configuration = configuration;
            _demoState = demoState;
        }

        public async Task<ApiResponseDTO<object>> LoginDemoAsync()
        {
            var email = _configuration["Demo:Email"];
            if (string.IsNullOrEmpty(email))
                return ApiResponseDTO<object>.ErrorResponse("Modo demonstração não configurado.", StatusCodes.Status500InternalServerError);

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return ApiResponseDTO<object>.ErrorResponse("Usuário de demonstração não encontrado.", StatusCodes.Status500InternalServerError);

            if (!user.AmbienteAtivoId.HasValue)
                return ApiResponseDTO<object>.ErrorResponse("Ambiente de demonstração não configurado.", StatusCodes.Status500InternalServerError);

            var ambienteId = user.AmbienteAtivoId.Value;

            // Reset diário: só limpa/recria uma vez por dia (controle em memória).
            if (_demoState.PrecisaResetar())
            {
                try
                {
                    await ResetarAmbienteAsync(ambienteId, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao resetar o ambiente de demonstração.");
                    // Não bloqueia o login — segue com os dados existentes.
                }
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("id", user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("ambiente_id", ambienteId.ToString())
            };

            foreach (var role in userRoles)
                authClaims.Add(new Claim(ClaimTypes.Role, role));

            var token = _tokenService.GenerateAccessToken(authClaims, _configuration);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);
            await _userManager.UpdateAsync(user);

            return ApiResponseDTO<object>.SuccessResponse(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = token.ValidTo
            });
        }

        /// <summary>
        /// Apaga todos os dados financeiros do ambiente demo e recria um conjunto
        /// de exemplo, para a demonstração nunca aparecer vazia.
        /// </summary>
        private async Task ResetarAmbienteAsync(Guid ambienteId, string usuarioId)
        {
            // Ordem respeita as dependências de FK (dependentes primeiro).
            await _db.Transacoes.Where(t => t.AmbienteId == ambienteId).ExecuteDeleteAsync();
            await _db.Orcamentos.Where(o => o.AmbienteId == ambienteId).ExecuteDeleteAsync();
            await _db.SaldoMensalContas.Where(s => s.AmbienteId == ambienteId).ExecuteDeleteAsync();
            await _db.Contas.Where(c => c.AmbienteId == ambienteId).ExecuteDeleteAsync();
            await _db.Categorias.Where(c => c.AmbienteId == ambienteId).ExecuteDeleteAsync();

            await SeedDadosExemploAsync(ambienteId, usuarioId);
        }

        private async Task SeedDadosExemploAsync(Guid ambienteId, string usuarioId)
        {
            var hoje = DateTime.Today;
            var mesCompetencia = new DateOnly(hoje.Year, hoje.Month, 1);
            int DiaDoMes(int dia) => Math.Min(dia, hoje.Day); // nunca lança transação no futuro

            // --- Categorias ---
            var catSalario = NovaCategoria(ambienteId, usuarioId, "Salário", "#16a34a");
            var catAlimentacao = NovaCategoria(ambienteId, usuarioId, "Alimentação", "#f97316");
            var catTransporte = NovaCategoria(ambienteId, usuarioId, "Transporte", "#3b82f6");
            var catMoradia = NovaCategoria(ambienteId, usuarioId, "Moradia", "#8b5cf6");
            var catLazer = NovaCategoria(ambienteId, usuarioId, "Lazer", "#ec4899");

            _db.Categorias.AddRange(catSalario, catAlimentacao, catTransporte, catMoradia, catLazer);

            // --- Contas ---
            var contaCorrente = NovaConta(ambienteId, usuarioId, "Conta Corrente", TipoConta.Corrente, 3200m);
            var contaPoupanca = NovaConta(ambienteId, usuarioId, "Poupança", TipoConta.Poupanca, 8500m);

            _db.Contas.AddRange(contaCorrente, contaPoupanca);

            // --- Transações de exemplo (mês atual) ---
            var transacoes = new List<Transacao>
            {
                NovaTransacao(ambienteId, usuarioId, "Salário", 5000m, TipoTransacao.Receita,
                    catSalario.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(5)), mesCompetencia),
                NovaTransacao(ambienteId, usuarioId, "Aluguel", 1500m, TipoTransacao.Despesa,
                    catMoradia.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(6)), mesCompetencia),
                NovaTransacao(ambienteId, usuarioId, "Supermercado", 650m, TipoTransacao.Despesa,
                    catAlimentacao.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(8)), mesCompetencia),
                NovaTransacao(ambienteId, usuarioId, "Combustível", 280m, TipoTransacao.Despesa,
                    catTransporte.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(10)), mesCompetencia),
                NovaTransacao(ambienteId, usuarioId, "Cinema", 90m, TipoTransacao.Despesa,
                    catLazer.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(12)), mesCompetencia),
                NovaTransacao(ambienteId, usuarioId, "Restaurante", 120m, TipoTransacao.Despesa,
                    catAlimentacao.Id, contaCorrente.Id, new DateTime(hoje.Year, hoje.Month, DiaDoMes(15)), mesCompetencia),
            };
            _db.Transacoes.AddRange(transacoes);

            // --- Orçamentos de exemplo ---
            var fimDoMes = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            _db.Orcamentos.AddRange(
                NovoOrcamento(ambienteId, usuarioId, "Alimentação do mês", catAlimentacao.Id, 1000m, fimDoMes),
                NovoOrcamento(ambienteId, usuarioId, "Lazer do mês", catLazer.Id, 300m, fimDoMes)
            );

            await _db.SaveChangesAsync();
        }

        private static Categoria NovaCategoria(Guid ambienteId, string usuarioId, string nome, string cor) => new()
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Cor = cor,
            UsuarioId = usuarioId,
            AmbienteId = ambienteId
        };

        private static Conta NovaConta(Guid ambienteId, string usuarioId, string nome, TipoConta tipo, decimal saldo) => new()
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            TipoConta = tipo,
            Saldo = saldo,
            UsuarioId = usuarioId,
            AmbienteId = ambienteId
        };

        private static Transacao NovaTransacao(Guid ambienteId, string usuarioId, string descricao, decimal valor,
            TipoTransacao tipo, Guid categoriaId, Guid contaId, DateTime data, DateOnly mesCompetencia) => new()
        {
            Id = Guid.NewGuid(),
            Descricao = descricao,
            Valor = valor,
            TipoTransacao = tipo,
            Data = data,
            MesCompetencia = mesCompetencia,
            CategoriaId = categoriaId,
            ContaId = contaId,
            UsuarioId = usuarioId,
            AmbienteId = ambienteId
        };

        private static Orcamento NovoOrcamento(Guid ambienteId, string usuarioId, string nome, Guid categoriaId,
            decimal valorLimite, DateTime dataLimite) => new()
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            ValorLimite = valorLimite,
            DataLimite = dataLimite,
            StatusOrcamento = StatusOrcamento.Ativo,
            CategoriaId = categoriaId,
            UsuarioId = usuarioId,
            AmbienteId = ambienteId
        };
    }
}
