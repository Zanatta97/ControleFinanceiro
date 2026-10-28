using ControleFinanceiroAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class TokenServiceTests
    {
        private const string ChaveSecreta = "chave-secreta-de-teste-com-mais-de-64-caracteres-para-hmac-sha512-ok";
        private const string Emissor = "emissor-teste";
        private const string Audiencia = "audiencia-teste";
        private const int ValidadeEmMinutos = 30;

        private readonly TokenService _service = new();

        private static IConfiguration CriarConfiguracao(string? chaveSecreta = ChaveSecreta)
        {
            var valores = new Dictionary<string, string?>
            {
                ["JWT:ValidAudience"] = Audiencia,
                ["JWT:ValidIssuer"] = Emissor,
                ["JWT:TokenValidityInMinutes"] = ValidadeEmMinutos.ToString()
            };

            if (chaveSecreta is not null)
                valores["JWT:SecretKey"] = chaveSecreta;

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        private static List<Claim> CriarClaims()
        {
            return
            [
                new Claim(ClaimTypes.Name, "usuario@teste.com"),
                new Claim("id", "usuario-1"),
                new Claim("ambiente_id", "11111111-1111-1111-1111-111111111111")
            ];
        }

        // Token já expirado, assinado com a chave e o algoritmo informados
        private static string CriarTokenExpirado(string chave, string algoritmo)
        {
            var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), algoritmo);
            var token = new JwtSecurityToken(
                issuer: Emissor,
                audience: Audiencia,
                claims: CriarClaims(),
                notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1),
                signingCredentials: credenciais);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #region GenerateAccessToken Tests
        [Fact]
        public void GenerateAccessToken_ComConfiguracaoValida_GeraTokenComEmissorAudienciaEClaims()
        {
            // Act
            var token = _service.GenerateAccessToken(CriarClaims(), CriarConfiguracao());

            // Assert
            Assert.Equal(Emissor, token.Issuer);
            Assert.Contains(Audiencia, token.Audiences);
            Assert.Contains(token.Claims, c => c.Type == "id" && c.Value == "usuario-1");
            Assert.Contains(token.Claims, c => c.Type == "ambiente_id" && c.Value == "11111111-1111-1111-1111-111111111111");
        }

        [Fact]
        public void GenerateAccessToken_ComValidadeConfigurada_ExpiraNoPrazo()
        {
            // Arrange
            var antes = DateTime.UtcNow;

            // Act
            var token = _service.GenerateAccessToken(CriarClaims(), CriarConfiguracao());

            // Assert — ValidTo tem precisão de segundos
            var depois = DateTime.UtcNow;
            Assert.InRange(token.ValidTo,
                antes.AddMinutes(ValidadeEmMinutos).AddSeconds(-1),
                depois.AddMinutes(ValidadeEmMinutos).AddSeconds(1));
        }

        [Fact]
        public void GenerateAccessToken_AssinaComHmacSha256()
        {
            // Act
            var token = _service.GenerateAccessToken(CriarClaims(), CriarConfiguracao());

            // Assert
            Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        }

        [Fact]
        public void GenerateAccessToken_SemSecretKey_LancaInvalidOperationException()
        {
            // Act
            var excecao = Assert.Throws<InvalidOperationException>(() =>
                _service.GenerateAccessToken(CriarClaims(), CriarConfiguracao(chaveSecreta: null)));

            // Assert
            Assert.Equal("SecretKey not found in configuration.", excecao.Message);
        }
        #endregion

        #region GenerateRefreshToken Tests
        [Fact]
        public void GenerateRefreshToken_GeraBase64De128Bytes()
        {
            // Act
            var refreshToken = _service.GenerateRefreshToken();

            // Assert
            var bytes = Convert.FromBase64String(refreshToken);
            Assert.Equal(128, bytes.Length);
        }

        [Fact]
        public void GenerateRefreshToken_ChamadasSeguidas_GeramValoresDiferentes()
        {
            // Act
            var tokens = Enumerable.Range(0, 20).Select(_ => _service.GenerateRefreshToken()).ToList();

            // Assert
            Assert.Equal(tokens.Count, tokens.Distinct().Count());
        }
        #endregion

        #region GetPrincipalFromExpiredToken Tests
        [Fact]
        public void GetPrincipalFromExpiredToken_ComTokenExpiradoValido_RetornaPrincipalComClaims()
        {
            // Arrange — o refresh aceita token vencido, desde que a assinatura confira
            var token = CriarTokenExpirado(ChaveSecreta, SecurityAlgorithms.HmacSha256);

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token, CriarConfiguracao());

            // Assert
            Assert.Equal("usuario@teste.com", principal.Identity!.Name);
            Assert.Equal("usuario-1", principal.FindFirstValue("id"));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ComTokenGeradoPeloServico_RetornaPrincipal()
        {
            // Arrange — ida e volta: o que o login gera, o refresh precisa aceitar
            var configuracao = CriarConfiguracao();
            var token = new JwtSecurityTokenHandler().WriteToken(_service.GenerateAccessToken(CriarClaims(), configuracao));

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token, configuracao);

            // Assert
            Assert.Equal("usuario-1", principal.FindFirstValue("id"));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ComAssinaturaDeOutraChave_LancaSecurityTokenException()
        {
            // Arrange
            var token = CriarTokenExpirado("outra-chave-secreta-com-tamanho-suficiente-para-hmac-sha256-xx", SecurityAlgorithms.HmacSha256);

            // Act / Assert
            Assert.ThrowsAny<SecurityTokenException>(() => _service.GetPrincipalFromExpiredToken(token, CriarConfiguracao()));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ComAlgoritmoDiferenteDeHs256_LancaSecurityTokenException()
        {
            // Arrange — assinatura válida, mas com HS512
            var token = CriarTokenExpirado(ChaveSecreta, SecurityAlgorithms.HmacSha512);

            // Act
            var excecao = Assert.Throws<SecurityTokenException>(() => _service.GetPrincipalFromExpiredToken(token, CriarConfiguracao()));

            // Assert
            Assert.Equal("Invalid token", excecao.Message);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_ComTokenMalformado_LancaExcecao()
        {
            // Act / Assert
            Assert.ThrowsAny<Exception>(() => _service.GetPrincipalFromExpiredToken("isto-nao-e-um-jwt", CriarConfiguracao()));
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_SemSecretKey_LancaInvalidOperationException()
        {
            // Arrange
            var token = CriarTokenExpirado(ChaveSecreta, SecurityAlgorithms.HmacSha256);

            // Act
            var excecao = Assert.Throws<InvalidOperationException>(() =>
                _service.GetPrincipalFromExpiredToken(token, CriarConfiguracao(chaveSecreta: null)));

            // Assert
            Assert.Equal("SecretKey not found in configuration.", excecao.Message);
        }
        #endregion
    }
}
