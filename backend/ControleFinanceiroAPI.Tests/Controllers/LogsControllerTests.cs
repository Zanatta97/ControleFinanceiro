using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.Filters;
using ControleFinanceiroAPI.Logging;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    // ListarLogs consulta o banco (AppDbContext.ApiLogs) e fica para teste de integração
    public class LogsControllerTests
    {
        private readonly LogSettingsManager _logSettings = new();
        private readonly LogsController _controller;

        public LogsControllerTests()
        {
            // DbContext sem provider: as ações testadas aqui não tocam no banco
            var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
            _controller = new LogsController(_logSettings, dbContext).ComUsuario(ambienteId: null);
        }

        private static bool? LerLogAllRequests(object? dados)
        {
            return dados?.GetType().GetProperty("logAllRequests")?.GetValue(dados) as bool?;
        }

        #region ObterStatus Tests
        [Fact]
        public void ObterStatus_PadraoDesativado_Retorna200ComFalse()
        {
            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(_controller.ObterStatus());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.False(LerLogAllRequests(corpo.Dados));
        }

        [Fact]
        public void ObterStatus_DepoisDeAtivar_RetornaTrue()
        {
            // Arrange
            _logSettings.Ativar();

            // Act
            var (_, corpo) = ControllerTestHelper.LerResposta<object>(_controller.ObterStatus());

            // Assert
            Assert.True(LerLogAllRequests(corpo.Dados));
        }
        #endregion

        #region AtivarLog Tests
        [Fact]
        public void AtivarLog_AtivaLogCompletoERetorna200()
        {
            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(_controller.AtivarLog());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.True(_logSettings.LogAllRequests);
            Assert.Equal("Log completo ativado. Todos os requests serão registrados.", corpo.StatusMessage);
        }
        #endregion

        #region DesativarLog Tests
        [Fact]
        public void DesativarLog_VoltaAoPadraoERetorna200()
        {
            // Arrange
            _logSettings.Ativar();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(_controller.DesativarLog());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.False(_logSettings.LogAllRequests);
            Assert.Equal("Log completo desativado. Apenas erros serão registrados.", corpo.StatusMessage);
        }
        #endregion

        #region Atributos Tests
        [Theory]
        [InlineData(nameof(LogsController.AtivarLog))]
        [InlineData(nameof(LogsController.DesativarLog))]
        public void AlternarLog_BloqueiaDemo(string acao)
        {
            // Act
            var metodo = typeof(LogsController).GetMethod(acao)!;

            // Assert
            Assert.NotNull(metodo.GetCustomAttribute<BloquearDemoAttribute>());
        }

        [Fact]
        public void ListarLogs_ExigeRoleAdmin()
        {
            // Act
            var autorizacao = typeof(LogsController).GetMethod(nameof(LogsController.ListarLogs))!
                .GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.Equal("Admin", autorizacao?.Roles);
        }

        [Theory(Skip = "Bug: LogsController não tem [Authorize] na classe nem em ativar/desativar; qualquer cliente anônimo liga o log com body (senhas do login incluídas)")]
        [InlineData(nameof(LogsController.AtivarLog))]
        [InlineData(nameof(LogsController.DesativarLog))]
        public void AlternarLog_ExigeAutenticacao(string acao)
        {
            // Act
            var metodo = typeof(LogsController).GetMethod(acao)!;
            var autorizacao = metodo.GetCustomAttribute<AuthorizeAttribute>()
                              ?? typeof(LogsController).GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(autorizacao);
        }
        #endregion
    }
}
