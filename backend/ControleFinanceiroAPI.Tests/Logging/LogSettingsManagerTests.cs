using ControleFinanceiroAPI.Logging;

namespace ControleFinanceiroAPI.Tests.Logging
{
    public class LogSettingsManagerTests
    {
        #region LogAllRequests Tests
        [Fact]
        public void LogAllRequests_NovaInstancia_ComecaDesativado()
        {
            // Act
            var manager = new LogSettingsManager();

            // Assert — padrão seguro: só erros, sem body
            Assert.False(manager.LogAllRequests);
        }
        #endregion

        #region Ativar Tests
        [Fact]
        public void Ativar_QuandoDesativado_PassaAAtivo()
        {
            // Arrange
            var manager = new LogSettingsManager();

            // Act
            manager.Ativar();

            // Assert
            Assert.True(manager.LogAllRequests);
        }

        [Fact]
        public void Ativar_DuasVezes_PermaneceAtivo()
        {
            // Arrange
            var manager = new LogSettingsManager();

            // Act
            manager.Ativar();
            manager.Ativar();

            // Assert
            Assert.True(manager.LogAllRequests);
        }
        #endregion

        #region Desativar Tests
        [Fact]
        public void Desativar_QuandoAtivo_VoltaAoPadrao()
        {
            // Arrange
            var manager = new LogSettingsManager();
            manager.Ativar();

            // Act
            manager.Desativar();

            // Assert
            Assert.False(manager.LogAllRequests);
        }

        [Fact]
        public void Desativar_QuandoJaDesativado_PermaneceDesativado()
        {
            // Arrange
            var manager = new LogSettingsManager();

            // Act
            manager.Desativar();

            // Assert
            Assert.False(manager.LogAllRequests);
        }
        #endregion
    }
}
