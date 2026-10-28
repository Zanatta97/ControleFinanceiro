using ControleFinanceiroAPI.Middleware;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ControleFinanceiroAPI.Tests.Middleware
{
    public class ErrorHandlingMiddlewareTests
    {
        private const string MensagemErroInterno = "Erro interno do servidor. Por favor, tente novamente mais tarde.";

        private static DefaultHttpContext CriarContexto()
        {
            var contexto = new DefaultHttpContext();
            contexto.Response.Body = new MemoryStream();
            return contexto;
        }

        private static async Task<JsonElement> LerCorpoAsync(HttpContext contexto)
        {
            contexto.Response.Body.Position = 0;
            using var leitor = new StreamReader(contexto.Response.Body);
            var json = await leitor.ReadToEndAsync();
            return JsonDocument.Parse(json).RootElement;
        }

        #region InvokeAsync Tests
        [Fact]
        public async Task InvokeAsync_SemExcecao_DeixaRespostaIntacta()
        {
            // Arrange
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            });

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            Assert.Equal(StatusCodes.Status201Created, contexto.Response.StatusCode);
            Assert.Equal(0, contexto.Response.Body.Length);
            Assert.False(contexto.Items.ContainsKey("Exception"));
        }

        public static TheoryData<Exception, int, string> ExcecoesMapeadas => new()
        {
            { new KeyNotFoundException("Conta não encontrada."), StatusCodes.Status404NotFound, "Conta não encontrada." },
            { new UnauthorizedAccessException("Nenhum ambiente selecionado."), StatusCodes.Status403Forbidden, "Nenhum ambiente selecionado." },
            { new InvalidOperationException("Este usuário já é membro do ambiente."), StatusCodes.Status409Conflict, "Este usuário já é membro do ambiente." },
        };

        [Theory]
        [MemberData(nameof(ExcecoesMapeadas))]
        public async Task InvokeAsync_ComExcecaoConhecida_DevolveStatusEMensagemDaExcecao(Exception excecao, int statusEsperado, string mensagemEsperada)
        {
            // Arrange
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(_ => throw excecao);

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            var corpo = await LerCorpoAsync(contexto);
            Assert.Equal(statusEsperado, contexto.Response.StatusCode);
            Assert.Equal("application/json", contexto.Response.ContentType);
            Assert.Equal(statusEsperado, corpo.GetProperty("statusCode").GetInt32());
            Assert.False(corpo.GetProperty("success").GetBoolean());
            Assert.Equal(mensagemEsperada, corpo.GetProperty("errorMessage").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ComArgumentNullException_Devolve400ComMensagemDaExcecao()
        {
            // Arrange
            var excecao = new ArgumentNullException("transacao");
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(_ => throw excecao);

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            var corpo = await LerCorpoAsync(contexto);
            Assert.Equal(StatusCodes.Status400BadRequest, contexto.Response.StatusCode);
            Assert.Equal(excecao.Message, corpo.GetProperty("errorMessage").GetString());
        }

        [Theory]
        [InlineData(typeof(Exception))]
        [InlineData(typeof(NullReferenceException))]
        [InlineData(typeof(ArgumentException))]
        [InlineData(typeof(FormatException))]
        public async Task InvokeAsync_ComExcecaoNaoMapeada_Devolve500ComMensagemGenerica(Type tipoExcecao)
        {
            // Arrange — detalhe interno não pode vazar para o cliente
            var excecao = (Exception)Activator.CreateInstance(tipoExcecao, "detalhe interno sensível")!;
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(_ => throw excecao);

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            var corpo = await LerCorpoAsync(contexto);
            Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
            Assert.Equal(StatusCodes.Status500InternalServerError, corpo.GetProperty("statusCode").GetInt32());
            Assert.Equal(MensagemErroInterno, corpo.GetProperty("errorMessage").GetString());
        }

        [Fact]
        public async Task InvokeAsync_ComExcecao_GuardaExcecaoNoContextoParaOLog()
        {
            // Arrange — o RequestLoggingMiddleware lê HttpContext.Items["Exception"]
            var excecao = new KeyNotFoundException("x");
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(_ => throw excecao);

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            Assert.Same(excecao, contexto.Items["Exception"]);
        }

        [Fact]
        public async Task InvokeAsync_ComExcecao_SerializaCorpoEmCamelCaseSemDados()
        {
            // Arrange
            var contexto = CriarContexto();
            var middleware = new ErrorHandlingMiddleware(_ => throw new KeyNotFoundException("x"));

            // Act
            await middleware.InvokeAsync(contexto);

            // Assert
            var corpo = await LerCorpoAsync(contexto);
            Assert.True(corpo.TryGetProperty("timestamp", out _));
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("dados").ValueKind);
            Assert.False(corpo.TryGetProperty("StatusCode", out _));
        }
        #endregion
    }
}
