using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Services
{
    public class AmbienteService : IAmbienteService
    {
        private readonly IUnityOfWork _repository;
        private readonly UserManager<Usuario> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AmbienteService> _logger;

        public AmbienteService(
            IUnityOfWork repository,
            UserManager<Usuario> userManager,
            ITokenService tokenService,
            IConfiguration configuration,
            ILogger<AmbienteService> logger)
        {
            _repository = repository;
            _userManager = userManager;
            _tokenService = tokenService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<Ambiente>> GetAllAsync()
        {
            return await _repository.AmbienteRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _repository.AmbienteRepository.GetAllByUsuarioAsync(usuarioId);
        }

        public async Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _repository.AmbienteRepository.GetAllByAmbienteAsync(ambienteId);
        }

        public async Task<Ambiente?> GetAsync(Guid id, string userId)
        {
            // Verifica se o usuário é membro antes de retornar
            var membro = await _repository.AmbienteRepository.GetMembroAsync(id, userId);
            if (membro is null) return null;

            return await _repository.AmbienteRepository.GetByIdReadOnlyAsync(a => a.Id == id);
        }

        public async Task<Ambiente> AddAsync(Ambiente ambiente, string userId)
        {
            ArgumentNullException.ThrowIfNull(ambiente, nameof(ambiente));

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            try
            {
                ambiente.Id = Guid.NewGuid();
                ambiente.UsuarioId = user.Id;
                ambiente.Usuario = user;
                ambiente.DataCriacao = DateTime.Now;

                _repository.AmbienteRepository.Add(ambiente);

                // Adiciona o criador como Dono automaticamente
                _repository.AmbienteRepository.AddMembro(new AmbienteMembro
                {
                    AmbienteId = ambiente.Id,
                    UsuarioId = user.Id,
                    Role = "Dono"
                });

                await _repository.SaveChangesAsync();
                return ambiente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar o ambiente");
                throw;
            }
        }

        public async Task<Ambiente> UpdateAsync(Guid id, Ambiente ambiente, string userId)
        {
            ArgumentNullException.ThrowIfNull(ambiente, nameof(ambiente));
            try
            {
                var solicitante = await _userManager.FindByIdAsync(userId)
                    ?? throw new KeyNotFoundException("Usuário não encontrado.");

                var isAdmin = await _userManager.IsInRoleAsync(solicitante, "Admin");

                // Admin do sistema ou Dono do ambiente podem editar
                if (!isAdmin)
                {
                    var membro = await _repository.AmbienteRepository.GetMembroAsync(id, solicitante.Id);
                    if (membro is null || membro.Role != "Dono")
                        throw new UnauthorizedAccessException("Apenas o Dono ou um Admin do sistema pode editar o ambiente.");
                }

                var ambienteExistente = await _repository.AmbienteRepository.GetByIdAsync(a => a.Id == id)
                    ?? throw new KeyNotFoundException("Ambiente não encontrado.");

                ambienteExistente.Nome = ambiente.Nome;

                _repository.AmbienteRepository.Update(ambienteExistente);
                await _repository.SaveChangesAsync();
                return ambienteExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar o ambiente");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, string userId)
        {
            try
            {
                // userId vazio indica deleção forçada por Admin do sistema
                if (string.IsNullOrEmpty(userId))
                {
                    var alvo = await _repository.AmbienteRepository.GetByIdAsync(a => a.Id == id);
                    if (alvo is null) return false;
                    _repository.AmbienteRepository.Delete(alvo);
                    await _repository.SaveChangesAsync();
                    return true;
                }

                var solicitante = await _userManager.FindByIdAsync(userId)
                    ?? throw new KeyNotFoundException("Usuário não encontrado.");

                var isAdmin = await _userManager.IsInRoleAsync(solicitante, "Admin");

                // Admin do sistema ou Dono do ambiente podem deletar
                if (!isAdmin)
                {
                    var membro = await _repository.AmbienteRepository.GetMembroAsync(id, solicitante.Id);
                    if (membro is null || membro.Role != "Dono")
                        throw new UnauthorizedAccessException("Apenas o Dono ou um Admin do sistema pode deletar o ambiente.");
                }

                var ambiente = await _repository.AmbienteRepository.GetByIdAsync(a => a.Id == id);
                if (ambiente is null) return false;

                _repository.AmbienteRepository.Delete(ambiente);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar o ambiente");
                throw;
            }
        }

        public async Task AtribuirDonoAsync(Guid ambienteId, string solicitanteUserId, string membroUserId)
        {
            try
            {
                var solicitante = await _userManager.FindByIdAsync(solicitanteUserId)
                    ?? throw new KeyNotFoundException("Usuário solicitante não encontrado.");

                var isAdmin = await _userManager.IsInRoleAsync(solicitante, "Admin");

                // Apenas Admin do sistema ou Dono atual podem transferir a posse
                if (!isAdmin)
                {
                    var membroSolicitante = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, solicitante.Id);
                    if (membroSolicitante is null || membroSolicitante.Role != "Dono")
                        throw new UnauthorizedAccessException("Apenas o Dono atual ou um Admin do sistema pode atribuir um novo Dono.");
                }

                // Busca o membro que vai se tornar o novo Dono
                var novoDonoMembro = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, membroUserId)
                    ?? throw new KeyNotFoundException("O usuário indicado não é membro deste ambiente.");

                // Rebaixa o Dono atual para Admin
                var donoAtual = await _repository.AmbienteRepository.GetByIdAsync(a => a.Id == ambienteId);
                var membroDonoAtual = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, donoAtual!.UsuarioId!);
                if (membroDonoAtual is not null && membroDonoAtual.UsuarioId != membroUserId)
                    membroDonoAtual.Role = "Admin";

                // Promove o novo Dono
                novoDonoMembro.Role = "Dono";

                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atribuir novo Dono ao ambiente");
                throw;
            }
        }

        public async Task AdicionarMembroAsync(Guid ambienteId, string solicitanteUserId, string membroEmail)
        {
            // Verifica se o solicitante tem permissão (Dono ou Admin)
            var solicitante = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, solicitanteUserId);
            if (solicitante is null || solicitante.Role == "Membro")
                throw new UnauthorizedAccessException("Apenas Donos e Admins podem adicionar membros.");

            // Busca o usuário a ser adicionado pelo e-mail
            var usuario = await _userManager.FindByEmailAsync(membroEmail)
                ?? throw new KeyNotFoundException($"Usuário com e-mail '{membroEmail}' não encontrado.");

            // Verifica se já é membro
            var membroExistente = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, usuario.Id);
            if (membroExistente is not null)
                throw new InvalidOperationException("Este usuário já é membro do ambiente.");

            _repository.AmbienteRepository.AddMembro(new AmbienteMembro
            {
                AmbienteId = ambienteId,
                UsuarioId = usuario.Id,
                Role = "Membro"
            });

            await _repository.SaveChangesAsync();
        }

        public async Task RemoverMembroAsync(Guid ambienteId, string solicitanteUserId, string membroUserId)
        {
            // Verifica se o solicitante tem permissão (Dono ou Admin)
            var solicitante = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, solicitanteUserId);
            if (solicitante is null || solicitante.Role == "Membro")
                throw new UnauthorizedAccessException("Apenas Donos e Admins podem remover membros.");

            var membro = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, membroUserId)
                ?? throw new KeyNotFoundException("Membro não encontrado neste ambiente.");

            // Impede remoção do próprio Dono
            if (membro.Role == "Dono")
                throw new InvalidOperationException("O Dono do ambiente não pode ser removido.");

            _repository.AmbienteRepository.RemoveMembro(membro);
            await _repository.SaveChangesAsync();
        }

        public async Task<TokenDTO> SelecionarAmbienteAsync(Guid ambienteId, string userId)
        {
            // Verifica se o usuário é membro do ambiente solicitado
            var membro = await _repository.AmbienteRepository.GetMembroAsync(ambienteId, userId)
                ?? throw new UnauthorizedAccessException("Você não é membro deste ambiente.");

            // Atualiza o ambiente ativo do usuário no banco
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            user.AmbienteAtivoId = ambienteId;
            await _userManager.UpdateAsync(user);

            // Reconstrói os claims com o novo ambiente_id e gera novo token
            var userRoles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("id", user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("ambiente_id", ambienteId.ToString())
            };
            foreach (var role in userRoles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var novoToken = _tokenService.GenerateAccessToken(claims, _configuration);
            var novoRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = novoRefreshToken;
            await _userManager.UpdateAsync(user);

            return new TokenDTO
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(novoToken),
                RefreshToken = novoRefreshToken
            };
        }
    }
}
