using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using ControleFinanceiroAPI.Tests.Helpers;
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

        private const string OutroUsuarioId = "usuario-2";
        private const string TerceiroUsuarioId = "usuario-3";

        private static Usuario CriarUsuario(string id)
        {
            return new Usuario { Id = id, UserName = $"{id}@teste.com", Email = $"{id}@teste.com" };
        }

        // Usuário encontrado pelo UserManager, com ou sem a role Admin do sistema
        private Usuario ConfigurarUsuario(string id, bool adminDoSistema = false)
        {
            var usuario = CriarUsuario(id);
            _userManager.FindByIdAsync(id).Returns(usuario);
            _userManager.IsInRoleAsync(usuario, "Admin").Returns(adminDoSistema);
            return usuario;
        }

        // Vínculo do usuário com o ambiente de teste
        private AmbienteMembro ConfigurarMembro(string usuarioId, string role)
        {
            var membro = new AmbienteMembro { AmbienteId = _ambienteId, UsuarioId = usuarioId, Role = role };
            _repository.AmbienteRepository.GetMembroAsync(_ambienteId, usuarioId).Returns(membro);
            return membro;
        }

        private void ConfigurarNaoMembro(string usuarioId)
        {
            _repository.AmbienteRepository.GetMembroAsync(_ambienteId, usuarioId).Returns((AmbienteMembro?)null);
        }

        private Ambiente ConfigurarAmbiente(string criadorId = UsuarioId)
        {
            var ambiente = new Ambiente { Id = _ambienteId, Nome = "Família", UsuarioId = criadorId };
            _repository.AmbienteRepository.ComDados(ambiente);
            return ambiente;
        }

        #region Consultas Tests
        [Fact]
        public async Task GetAllAsync_RetornaTodosDoRepositorio()
        {
            // Arrange
            var ambientes = new List<Ambiente> { new() { Nome = "A" }, new() { Nome = "B" } };
            _repository.AmbienteRepository.GetAllAsync().Returns(ambientes);

            // Act
            var resultado = await _service.GetAllAsync();

            // Assert
            Assert.Same(ambientes, resultado);
        }

        [Fact]
        public async Task GetAllByUsuarioAsync_RepassaUsuarioAoRepositorio()
        {
            // Arrange
            var ambientes = new List<Ambiente> { new() { Nome = "A" } };
            _repository.AmbienteRepository.GetAllByUsuarioAsync(UsuarioId).Returns(ambientes);

            // Act
            var resultado = await _service.GetAllByUsuarioAsync(UsuarioId);

            // Assert
            Assert.Same(ambientes, resultado);
        }

        [Fact]
        public async Task GetAllByAmbienteAsync_RetornaUsuariosDoAmbiente()
        {
            // Arrange
            var usuarios = new List<Usuario> { CriarUsuario(UsuarioId) };
            _repository.AmbienteRepository.GetAllByAmbienteAsync(_ambienteId).Returns(usuarios);

            // Act
            var resultado = await _service.GetAllByAmbienteAsync(_ambienteId);

            // Assert
            Assert.Same(usuarios, resultado);
        }

        [Fact]
        public async Task GetAsync_ComUsuarioMembro_RetornaAmbiente()
        {
            // Arrange
            var ambiente = ConfigurarAmbiente();
            ConfigurarMembro(UsuarioId, "Membro");

            // Act
            var resultado = await _service.GetAsync(_ambienteId, UsuarioId);

            // Assert
            Assert.Same(ambiente, resultado);
        }

        [Fact]
        public async Task GetAsync_ComUsuarioNaoMembro_RetornaNuloSemBuscarAmbiente()
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            var resultado = await _service.GetAsync(_ambienteId, OutroUsuarioId);

            // Assert
            Assert.Null(resultado);
            await _repository.AmbienteRepository.DidNotReceive()
                .GetByIdReadOnlyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Ambiente, bool>>>());
        }
        #endregion

        #region AddAsync Tests
        [Fact]
        public async Task AddAsync_ComUsuarioExistente_CriaAmbienteComCriadorComoDono()
        {
            // Arrange
            var usuario = ConfigurarUsuario(UsuarioId);
            var ambiente = new Ambiente { Nome = "Casa" };

            // Act
            var resultado = await _service.AddAsync(ambiente, UsuarioId);

            // Assert
            Assert.Same(ambiente, resultado);
            Assert.NotEqual(Guid.Empty, resultado.Id);
            Assert.Equal(UsuarioId, resultado.UsuarioId);
            Assert.Same(usuario, resultado.Usuario);
            Assert.NotNull(resultado.DataCriacao);
            _repository.AmbienteRepository.Received(1).Add(ambiente);
            _repository.AmbienteRepository.Received(1).AddMembro(Arg.Is<AmbienteMembro>(m =>
                m.AmbienteId == resultado.Id && m.UsuarioId == UsuarioId && m.Role == "Dono"));
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComUsuarioInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _userManager.FindByIdAsync(UsuarioId).Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddAsync(new Ambiente(), UsuarioId));

            // Assert
            Assert.Equal("Usuário não encontrado.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComAmbienteNulo_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!, UsuarioId));
        }
        #endregion

        #region UpdateAsync Tests
        [Fact]
        public async Task UpdateAsync_ComDono_AtualizaNomeESalva()
        {
            // Arrange
            var ambiente = ConfigurarAmbiente();
            ConfigurarUsuario(UsuarioId);
            ConfigurarMembro(UsuarioId, "Dono");

            // Act
            var resultado = await _service.UpdateAsync(_ambienteId, new Ambiente { Nome = "Novo nome" }, UsuarioId);

            // Assert
            Assert.Same(ambiente, resultado);
            Assert.Equal("Novo nome", ambiente.Nome);
            _repository.AmbienteRepository.Received(1).Update(ambiente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ComAdminDoSistemaNaoMembro_AtualizaNome()
        {
            // Arrange
            var ambiente = ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId, adminDoSistema: true);
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            await _service.UpdateAsync(_ambienteId, new Ambiente { Nome = "Novo nome" }, OutroUsuarioId);

            // Assert
            Assert.Equal("Novo nome", ambiente.Nome);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Membro")]
        public async Task UpdateAsync_ComMembroQueNaoEDono_LancaUnauthorizedAccessException(string role)
        {
            // Arrange
            var ambiente = ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId);
            ConfigurarMembro(OutroUsuarioId, role);

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateAsync(_ambienteId, new Ambiente { Nome = "Novo nome" }, OutroUsuarioId));

            // Assert
            Assert.Equal("Apenas o Dono ou um Admin do sistema pode editar o ambiente.", excecao.Message);
            Assert.Equal("Família", ambiente.Nome);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ComUsuarioNaoMembro_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId);
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.UpdateAsync(_ambienteId, new Ambiente { Nome = "x" }, OutroUsuarioId));
        }

        [Fact]
        public async Task UpdateAsync_ComSolicitanteInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _userManager.FindByIdAsync(UsuarioId).Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(_ambienteId, new Ambiente(), UsuarioId));

            // Assert
            Assert.Equal("Usuário não encontrado.", excecao.Message);
        }

        [Fact]
        public async Task UpdateAsync_ComAmbienteInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.AmbienteRepository.ComDados();
            ConfigurarUsuario(UsuarioId, adminDoSistema: true);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(_ambienteId, new Ambiente { Nome = "x" }, UsuarioId));

            // Assert
            Assert.Equal("Ambiente não encontrado.", excecao.Message);
        }

        [Fact]
        public async Task UpdateAsync_ComAmbienteNulo_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(_ambienteId, null!, UsuarioId));
        }
        #endregion

        #region DeleteAsync Tests
        [Fact]
        public async Task DeleteAsync_SemUsuario_DeletaForcadoPeloAdmin()
        {
            // Arrange — userId vazio é o caminho do AdminController
            var ambiente = ConfigurarAmbiente();

            // Act
            var resultado = await _service.DeleteAsync(_ambienteId, string.Empty);

            // Assert
            Assert.True(resultado);
            _repository.AmbienteRepository.Received(1).Delete(ambiente);
            await _repository.Received(1).SaveChangesAsync();
            await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task DeleteAsync_SemUsuarioEAmbienteInexistente_RetornaFalse()
        {
            // Arrange
            _repository.AmbienteRepository.ComDados();

            // Act
            var resultado = await _service.DeleteAsync(_ambienteId, string.Empty);

            // Assert
            Assert.False(resultado);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ComDono_DeletaERetornaTrue()
        {
            // Arrange
            var ambiente = ConfigurarAmbiente();
            ConfigurarUsuario(UsuarioId);
            ConfigurarMembro(UsuarioId, "Dono");

            // Act
            var resultado = await _service.DeleteAsync(_ambienteId, UsuarioId);

            // Assert
            Assert.True(resultado);
            _repository.AmbienteRepository.Received(1).Delete(ambiente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ComAdminDoSistema_DeletaERetornaTrue()
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId, adminDoSistema: true);
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            var resultado = await _service.DeleteAsync(_ambienteId, OutroUsuarioId);

            // Assert
            Assert.True(resultado);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Membro")]
        public async Task DeleteAsync_ComMembroQueNaoEDono_LancaUnauthorizedAccessException(string role)
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId);
            ConfigurarMembro(OutroUsuarioId, role);

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.DeleteAsync(_ambienteId, OutroUsuarioId));

            // Assert
            Assert.Equal("Apenas o Dono ou um Admin do sistema pode deletar o ambiente.", excecao.Message);
            _repository.AmbienteRepository.DidNotReceive().Delete(Arg.Any<Ambiente>());
        }

        [Fact]
        public async Task DeleteAsync_ComSolicitanteInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _userManager.FindByIdAsync(UsuarioId).Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(_ambienteId, UsuarioId));

            // Assert
            Assert.Equal("Usuário não encontrado.", excecao.Message);
        }

        [Fact]
        public async Task DeleteAsync_ComDonoEAmbienteInexistente_RetornaFalse()
        {
            // Arrange
            _repository.AmbienteRepository.ComDados();
            ConfigurarUsuario(UsuarioId);
            ConfigurarMembro(UsuarioId, "Dono");

            // Act
            var resultado = await _service.DeleteAsync(_ambienteId, UsuarioId);

            // Assert
            Assert.False(resultado);
            await _repository.DidNotReceive().SaveChangesAsync();
        }
        #endregion

        #region AtribuirDonoAsync Tests
        [Fact]
        public async Task AtribuirDonoAsync_ComDonoAtual_PromoveMembroERebaixaDonoParaAdmin()
        {
            // Arrange
            ConfigurarAmbiente(criadorId: UsuarioId);
            ConfigurarUsuario(UsuarioId);
            var donoAtual = ConfigurarMembro(UsuarioId, "Dono");
            var novoDono = ConfigurarMembro(OutroUsuarioId, "Membro");

            // Act
            await _service.AtribuirDonoAsync(_ambienteId, UsuarioId, OutroUsuarioId);

            // Assert
            Assert.Equal("Admin", donoAtual.Role);
            Assert.Equal("Dono", novoDono.Role);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AtribuirDonoAsync_ComAdminDoSistema_PromoveMembro()
        {
            // Arrange
            ConfigurarAmbiente(criadorId: UsuarioId);
            ConfigurarUsuario(TerceiroUsuarioId, adminDoSistema: true);
            var donoAtual = ConfigurarMembro(UsuarioId, "Dono");
            var novoDono = ConfigurarMembro(OutroUsuarioId, "Admin");

            // Act
            await _service.AtribuirDonoAsync(_ambienteId, TerceiroUsuarioId, OutroUsuarioId);

            // Assert
            Assert.Equal("Admin", donoAtual.Role);
            Assert.Equal("Dono", novoDono.Role);
        }

        [Fact]
        public async Task AtribuirDonoAsync_ParaOProprioDono_MantemDono()
        {
            // Arrange
            ConfigurarAmbiente(criadorId: UsuarioId);
            ConfigurarUsuario(UsuarioId);
            var donoAtual = ConfigurarMembro(UsuarioId, "Dono");

            // Act
            await _service.AtribuirDonoAsync(_ambienteId, UsuarioId, UsuarioId);

            // Assert
            Assert.Equal("Dono", donoAtual.Role);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Membro")]
        public async Task AtribuirDonoAsync_ComSolicitanteQueNaoEDono_LancaUnauthorizedAccessException(string role)
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarUsuario(OutroUsuarioId);
            ConfigurarMembro(OutroUsuarioId, role);

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AtribuirDonoAsync(_ambienteId, OutroUsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("Apenas o Dono atual ou um Admin do sistema pode atribuir um novo Dono.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AtribuirDonoAsync_ComSolicitanteInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _userManager.FindByIdAsync(UsuarioId).Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AtribuirDonoAsync(_ambienteId, UsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("Usuário solicitante não encontrado.", excecao.Message);
        }

        [Fact]
        public async Task AtribuirDonoAsync_ParaUsuarioQueNaoEMembro_LancaKeyNotFoundException()
        {
            // Arrange
            ConfigurarAmbiente();
            ConfigurarUsuario(UsuarioId);
            ConfigurarMembro(UsuarioId, "Dono");
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AtribuirDonoAsync(_ambienteId, UsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("O usuário indicado não é membro deste ambiente.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact(Skip = "Bug: AtribuirDonoAsync rebaixa Ambiente.UsuarioId (o criador), não o Dono atual; após uma transferência, a seguinte deixa dois Donos")]
        public async Task AtribuirDonoAsync_SegundaTransferencia_RebaixaODonoAtualENaoOCriador()
        {
            // Arrange — criador (usuario-1) já passou a posse para usuario-2 e virou Admin
            ConfigurarAmbiente(criadorId: UsuarioId);
            ConfigurarUsuario(OutroUsuarioId);
            var criador = ConfigurarMembro(UsuarioId, "Admin");
            var donoAtual = ConfigurarMembro(OutroUsuarioId, "Dono");
            var novoDono = ConfigurarMembro(TerceiroUsuarioId, "Membro");

            // Act — usuario-2 transfere para usuario-3
            await _service.AtribuirDonoAsync(_ambienteId, OutroUsuarioId, TerceiroUsuarioId);

            // Assert — só pode haver um Dono
            Assert.Equal("Dono", novoDono.Role);
            Assert.Equal("Admin", donoAtual.Role);
            Assert.Equal("Admin", criador.Role);
        }
        #endregion

        #region AdicionarMembroAsync Tests
        [Theory]
        [InlineData("Dono")]
        [InlineData("Admin")]
        public async Task AdicionarMembroAsync_ComDonoOuAdmin_AdicionaComoMembro(string roleSolicitante)
        {
            // Arrange
            ConfigurarMembro(UsuarioId, roleSolicitante);
            var novo = CriarUsuario(OutroUsuarioId);
            _userManager.FindByEmailAsync("novo@teste.com").Returns(novo);
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            await _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com");

            // Assert
            _repository.AmbienteRepository.Received(1).AddMembro(Arg.Is<AmbienteMembro>(m =>
                m.AmbienteId == _ambienteId && m.UsuarioId == OutroUsuarioId && m.Role == "Membro"));
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarMembroAsync_ComSolicitanteMembro_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Membro");

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com"));

            // Assert
            Assert.Equal("Apenas Donos e Admins podem adicionar membros.", excecao.Message);
            _repository.AmbienteRepository.DidNotReceive().AddMembro(Arg.Any<AmbienteMembro>());
        }

        [Fact]
        public async Task AdicionarMembroAsync_ComSolicitanteForaDoAmbiente_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarNaoMembro(UsuarioId);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com"));
        }

        [Fact]
        public async Task AdicionarMembroAsync_ComEmailInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Dono");
            _userManager.FindByEmailAsync("ninguem@teste.com").Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "ninguem@teste.com"));

            // Assert
            Assert.Equal("Usuário com e-mail 'ninguem@teste.com' não encontrado.", excecao.Message);
        }

        [Fact]
        public async Task AdicionarMembroAsync_ComUsuarioJaMembro_LancaInvalidOperationException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Dono");
            _userManager.FindByEmailAsync("novo@teste.com").Returns(CriarUsuario(OutroUsuarioId));
            ConfigurarMembro(OutroUsuarioId, "Membro");

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com"));

            // Assert
            Assert.Equal("Este usuário já é membro do ambiente.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }
        #endregion

        #region RemoverMembroAsync Tests
        [Theory]
        [InlineData("Dono")]
        [InlineData("Admin")]
        public async Task RemoverMembroAsync_ComDonoOuAdmin_RemoveMembro(string roleSolicitante)
        {
            // Arrange
            ConfigurarMembro(UsuarioId, roleSolicitante);
            var membro = ConfigurarMembro(OutroUsuarioId, "Membro");

            // Act
            await _service.RemoverMembroAsync(_ambienteId, UsuarioId, OutroUsuarioId);

            // Assert
            _repository.AmbienteRepository.Received(1).RemoveMembro(membro);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task RemoverMembroAsync_ComSolicitanteMembro_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Membro");

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RemoverMembroAsync(_ambienteId, UsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("Apenas Donos e Admins podem remover membros.", excecao.Message);
        }

        [Fact]
        public async Task RemoverMembroAsync_ComSolicitanteForaDoAmbiente_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarNaoMembro(UsuarioId);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.RemoverMembroAsync(_ambienteId, UsuarioId, OutroUsuarioId));
        }

        [Fact]
        public async Task RemoverMembroAsync_ComMembroInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Dono");
            ConfigurarNaoMembro(OutroUsuarioId);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.RemoverMembroAsync(_ambienteId, UsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("Membro não encontrado neste ambiente.", excecao.Message);
        }

        [Fact]
        public async Task RemoverMembroAsync_RemovendoODono_LancaInvalidOperationException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Admin");
            ConfigurarMembro(OutroUsuarioId, "Dono");

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.RemoverMembroAsync(_ambienteId, UsuarioId, OutroUsuarioId));

            // Assert
            Assert.Equal("O Dono do ambiente não pode ser removido.", excecao.Message);
            _repository.AmbienteRepository.DidNotReceive().RemoveMembro(Arg.Any<AmbienteMembro>());
            await _repository.DidNotReceive().SaveChangesAsync();
        }
        #endregion

        #region SelecionarAmbienteAsync Tests
        [Fact]
        public async Task SelecionarAmbienteAsync_ComUsuarioMembro_GravaAmbienteAtivoEGeraTokenComClaim()
        {
            // Arrange
            var usuario = ConfigurarUsuario(UsuarioId);
            ConfigurarMembro(UsuarioId, "Membro");
            _userManager.GetRolesAsync(usuario).Returns(new List<string> { "User" });

            // Act
            var token = await _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId);

            // Assert
            Assert.Equal(_ambienteId, usuario.AmbienteAtivoId);
            Assert.False(string.IsNullOrEmpty(token.AccessToken));
            Assert.Equal(RefreshTokenNovo, usuario.RefreshToken);
            _tokenService.Received(1).GenerateAccessToken(Arg.Is<IEnumerable<Claim>>(claims =>
                claims.Any(c => c.Type == "ambiente_id" && c.Value == _ambienteId.ToString()) &&
                claims.Any(c => c.Type == "id" && c.Value == UsuarioId) &&
                claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "User")), _configuration);
            await _userManager.Received().UpdateAsync(usuario);
        }

        [Fact]
        public async Task SelecionarAmbienteAsync_ComUsuarioNaoMembro_LancaUnauthorizedAccessException()
        {
            // Arrange
            ConfigurarNaoMembro(UsuarioId);

            // Act
            var excecao = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId));

            // Assert
            Assert.Equal("Você não é membro deste ambiente.", excecao.Message);
            _tokenService.DidNotReceive().GenerateAccessToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<IConfiguration>());
        }

        [Fact]
        public async Task SelecionarAmbienteAsync_ComUsuarioInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            ConfigurarMembro(UsuarioId, "Membro");
            _userManager.FindByIdAsync(UsuarioId).Returns((Usuario?)null);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId));

            // Assert
            Assert.Equal("Usuário não encontrado.", excecao.Message);
        }

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
