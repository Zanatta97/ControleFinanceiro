using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Filters;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Reflection;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly IUsuarioService _service = Substitute.For<IUsuarioService>();
        private readonly IDemoService _demoService = Substitute.For<IDemoService>();
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _controller = new AuthController(_service, _demoService).ComUsuario(ambienteId: null);
        }

        // O AuthController devolve o ApiResponseDTO do serviço com o status que ele trouxe
        private static void AssertRepassaResposta(IActionResult resultado, ApiResponseDTO<object> esperado)
        {
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(resultado);
            Assert.Equal(esperado.StatusCode, status);
            Assert.Same(esperado, corpo);
        }

        #region Login Tests
        [Theory]
        [InlineData(StatusCodes.Status200OK)]
        [InlineData(StatusCodes.Status401Unauthorized)]
        public async Task Login_RepassaStatusDoServico(int statusServico)
        {
            // Arrange
            var dto = new UsuarioLoginRequestDTO { Email = "a@teste.com", Senha = "x" };
            var resposta = statusServico == StatusCodes.Status200OK
                ? ApiResponseDTO<object>.SuccessResponse(new { Token = "t" })
                : ApiResponseDTO<object>.ErrorResponse("Erro ao fazer login", statusServico);
            _service.Login(dto).Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.Login(dto), resposta);
        }
        #endregion

        #region LoginDemo Tests
        [Fact]
        public async Task LoginDemo_RepassaRespostaDoDemoService()
        {
            // Arrange
            var resposta = ApiResponseDTO<object>.SuccessResponse(new { Token = "demo" });
            _demoService.LoginDemoAsync().Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.LoginDemo(), resposta);
        }
        #endregion

        #region Roles Tests
        [Fact]
        public async Task CreateRole_RepassaRespostaDoServico()
        {
            // Arrange
            var resposta = ApiResponseDTO<object>.ErrorResponse("Role 'Admin' já existe.", StatusCodes.Status400BadRequest);
            _service.CreateRole("Admin").Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.CreateRole("Admin"), resposta);
        }

        [Fact]
        public async Task AssignUserToRole_RepassaRespostaDoServico()
        {
            // Arrange
            var resposta = ApiResponseDTO<object>.SuccessResponse("ok");
            _service.AssignUserToRole("a@teste.com", "Admin").Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.AssignUserToRole("a@teste.com", "Admin"), resposta);
        }
        #endregion

        #region Register Tests
        [Fact]
        public async Task Register_RepassaRespostaDoServico()
        {
            // Arrange
            var dto = new UsuarioRegisterDTO { Nome = "Fulano", Email = "f@teste.com", Senha = "x", ConfirmacaoSenha = "x" };
            var resposta = ApiResponseDTO<object>.ErrorResponse("Usuário de e-mail f@teste.com já existe.", StatusCodes.Status400BadRequest);
            _service.Register(dto).Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.Register(dto), resposta);
        }
        #endregion

        #region AlterarSenha Tests
        [Fact]
        public async Task AlterarSenha_ComUsuarioNoToken_RepassaIdAoServico()
        {
            // Arrange
            var dto = new AlterarSenhaDTO { SenhaAtual = "a", NovaSenha = "b", ConfirmacaoNovaSenha = "b" };
            var resposta = ApiResponseDTO<object>.SuccessResponse("Senha alterada com sucesso.");
            _service.AlterarSenha(ControllerTestHelper.UsuarioId, dto).Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.AlterarSenha(dto), resposta);
        }

        [Fact]
        public async Task AlterarSenha_SemUsuarioNoToken_Retorna401SemChamarServico()
        {
            // Arrange
            var controller = new AuthController(_service, _demoService).ComUsuario(ambienteId: null, usuarioId: null);
            var dto = new AlterarSenhaDTO { SenhaAtual = "a", NovaSenha = "b", ConfirmacaoNovaSenha = "b" };

            // Act
            var resultado = await controller.AlterarSenha(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(resultado);
            await _service.DidNotReceive().AlterarSenha(Arg.Any<string>(), Arg.Any<AlterarSenhaDTO>());
        }
        #endregion

        #region RefreshToken Tests
        [Fact]
        public async Task RefreshToken_RepassaRespostaDoServico()
        {
            // Arrange
            var dto = new TokenDTO { AccessToken = "a", RefreshToken = "r" };
            var resposta = ApiResponseDTO<object>.ErrorResponse("Token de atualização inválido", StatusCodes.Status400BadRequest);
            _service.RefreshToken(dto).Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.RefreshToken(dto), resposta);
        }
        #endregion

        #region Revoke Tests
        [Fact]
        public async Task Revoke_RepassaRespostaDoServico()
        {
            // Arrange
            var resposta = ApiResponseDTO<object>.SuccessResponse("Token revogado com sucesso");
            _service.Revoke("a@teste.com").Returns(resposta);

            // Act / Assert
            AssertRepassaResposta(await _controller.Revoke("a@teste.com"), resposta);
        }
        #endregion

        #region Atributos Tests
        [Theory]
        [InlineData(nameof(AuthController.CreateRole))]
        [InlineData(nameof(AuthController.AssignUserToRole))]
        [InlineData(nameof(AuthController.Register))]
        [InlineData(nameof(AuthController.Revoke))]
        public void AcoesAdministrativas_ExigemRoleAdmin(string acao)
        {
            // Act
            var autorizacao = typeof(AuthController).GetMethod(acao)!.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.Equal("Admin", autorizacao?.Roles);
        }

        [Fact]
        public void AlterarSenha_ExigeAutenticacaoEBloqueiaDemo()
        {
            // Act
            var metodo = typeof(AuthController).GetMethod(nameof(AuthController.AlterarSenha))!;

            // Assert
            Assert.NotNull(metodo.GetCustomAttribute<AuthorizeAttribute>());
            Assert.NotNull(metodo.GetCustomAttribute<BloquearDemoAttribute>());
        }
        #endregion
    }
}
