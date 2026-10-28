using ControleFinanceiroAPI.Services;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class DemoStateManagerTests
    {
        #region PrecisaResetar Tests
        [Fact]
        public void PrecisaResetar_PrimeiraChamadaDoDia_RetornaTrue()
        {
            // Arrange
            var manager = new DemoStateManager();

            // Act
            var resultado = manager.PrecisaResetar();

            // Assert
            Assert.True(resultado);
        }

        [Fact]
        public void PrecisaResetar_SegundaChamadaNoMesmoDia_RetornaFalse()
        {
            // Arrange
            var manager = new DemoStateManager();
            manager.PrecisaResetar();

            // Act
            var resultado = manager.PrecisaResetar();

            // Assert
            Assert.False(resultado);
        }

        [Fact]
        public async Task PrecisaResetar_ChamadasConcorrentes_SoUmaRetornaTrue()
        {
            // Arrange — várias requisições demo chegando ao mesmo tempo
            var manager = new DemoStateManager();

            // Act
            var resultados = await Task.WhenAll(Enumerable.Range(0, 50)
                .Select(_ => Task.Run(manager.PrecisaResetar)));

            // Assert — só uma delas dispara o reset
            Assert.Equal(1, resultados.Count(r => r));
        }
        #endregion
    }
}
