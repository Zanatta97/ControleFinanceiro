using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class AmbienteServiceTests
    {
        private const int ValidadeRefreshTokenEmMinutos = 60;
        private const string UsuarioId = "usuario-1";
        private const string RefreshTokenNovo = "refresh-token-novo";

        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly UserManager<Usuario> _userManager = Substitute.For<UserManager<Usuario>>(
            Substitute.For<IUserStore<Usuario>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly AmbienteService _service;
        private readonly Guid _ambienteId = new("11111111-1111-1111-1111-111111111111");

        public AmbienteServiceTests()
        {
            _configuration["JWT:RefreshTokenValidityInMinutes"].Returns(ValidadeRefreshTokenEmMinutos.ToString());

            _tokenService.GenerateAccessToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<IConfiguration>())
                .Returns(new JwtSecurityToken());
            _tokenService.GenerateRefreshToken().Returns(RefreshTokenNovo);

            _userManager.GetRolesAsync(Arg.Any<Usuario>()).Returns(new List<string>());
            _userManager.UpdateAsync(Arg.Any<Usuario>()).Returns(IdentityResult.Success);

            _service = new AmbienteService(
                _repository,
                _userManager,
                _tokenService,
                _configuration,
                Substitute.For<ILogger<AmbienteService>>());
        }

        #region SelecionarAmbienteAsync Tests
        [Fact]
        public async Task SelecionarAmbienteAsync_ComUsuarioMembro_EstendeValidadeDoRefreshToken()
        {
            // Arrange — validade atual termina em 5 minutos
            var antes = DateTime.UtcNow;
            var validadeAtual = antes.AddMinutes(5);
            var usuario = new Usuario
            {
                Id = UsuarioId,
                UserName = "usuario@teste.com",
                Email = "usuario@teste.com",
                RefreshToken = "refresh-token-salvo",
                RefreshTokenExpiryTime = validadeAtual
            };

            _repository.AmbienteRepository.GetMembroAsync(_ambienteId, UsuarioId)
                .Returns(new AmbienteMembro { AmbienteId = _ambienteId, UsuarioId = UsuarioId, Role = "Dono" });
            _userManager.FindByIdAsync(UsuarioId).Returns(usuario);

            // Act
            var token = await _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId);

            var depois = DateTime.UtcNow;

            // Assert — token rotacionado ganha validade nova, contada a partir de agora
            Assert.Equal(RefreshTokenNovo, token.RefreshToken);
            Assert.InRange(usuario.RefreshTokenExpiryTime,
                antes.AddMinutes(ValidadeRefreshTokenEmMinutos),
                depois.AddMinutes(ValidadeRefreshTokenEmMinutos));
            Assert.True(usuario.RefreshTokenExpiryTime > validadeAtual);
        }
        #endregion
    }
}
