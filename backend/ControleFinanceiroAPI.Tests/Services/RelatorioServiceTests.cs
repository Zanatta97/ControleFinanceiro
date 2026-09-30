using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class RelatorioServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly RelatorioService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();

        public RelatorioServiceTests()
        {
            _service = new RelatorioService(_repository);
        }
        #region GetResumoAsync Tests
        [Fact]
        public async Task GetResumoAsync_ComDespesaNoCartao_SeparaDasOutrasEIgnoraTransferencia()
        {
            // Arrange — monta o cenário
            var corrente = new Conta { TipoConta = TipoConta.Corrente };
            var cartao = new Conta { TipoConta = TipoConta.CartaoCredito };

            var transacoes = new List<Transacao>
        {
            new() { TipoTransacao = TipoTransacao.Receita,       Valor = 5000, Conta = corrente },
            new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 300,  Conta = corrente },
            new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 700,  Conta = cartao },
            new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 700,  Conta = corrente , ContaDestino = cartao }, // pagamento da fatura
        };

            _repository.TransacaoRepository
                .GetByMesCompetenciaAsync(_ambienteId, 9, 2026)
                .Returns(transacoes);

            // Act — executa o que está sendo testado
            var resumo = await _service.GetResumoAsync(_ambienteId, 9, 2026);

            // Assert — confere o resultado
            Assert.Equal(700, resumo.DespesasCartao); //Resultado apenas do valor do cartão
            Assert.Equal(300, resumo.DespesasOutras); //Resultado sem a transferencia
            Assert.Equal(1000, resumo.TotalDespesas); //Resultado da soma das despesas
            Assert.Equal(4000, resumo.Saldo); //Resultado do saldo (receita - despesas)
        }
        #endregion
        #region GetGastoPorCategoriaAsync Tests
        [Fact]
        public async Task GetGastosPorCategoriaAsync_ComMesAnoEPeriodo_LancaArgumentException()
        {

            //Arrange
            int mes = 9;
            int ano = 2026;
            DateTime dataInicial = DateTime.Today.AddDays(-10);
            DateTime dataFinal = DateTime.Today;

            //Act
            var execao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetGastoPorCategoriaAsync(_ambienteId, mes, ano, dataInicial, dataFinal));

            Assert.Equal("Informe apenas mês/ano ou período de datas, não ambos.", execao.Message);
        }

        [Fact]
        public async Task GetGastosPorCategoriaAsync_SemArgumentos_LancaArgumentException()
        {

            //Act
            var execao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetGastoPorCategoriaAsync(_ambienteId));

            Assert.Equal("Informe mês/ano ou período de datas.", execao.Message);
        }

        [Fact]
        public async Task GetGastosPorCategoriaAsync_RetornaPorPeriodo()
        {
            //Arrange
            DateTime dataInicial = new DateTime(2026, 9, 1);
            DateTime dataFinal = new DateTime(2026, 9, 30);

            var CategoriaAlimentacao = new Categoria {Id = Guid.NewGuid(), Nome = "Alimentação" };
            var CategoriaTransporte = new Categoria { Id = Guid.NewGuid(), Nome = "Transporte" };
            var CategoriaLazer = new Categoria { Id = Guid.NewGuid(),  Nome = "Lazer" };

            var transacoes = new List<Transacao>
        {
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 200, CategoriaId = CategoriaAlimentacao.Id, Categoria = CategoriaAlimentacao },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, CategoriaId = CategoriaTransporte.Id, Categoria = CategoriaTransporte },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 500, CategoriaId = CategoriaLazer.Id, Categoria = CategoriaLazer },
        };
            _repository.TransacaoRepository
                .GetByPeriodoAsync(_ambienteId, dataInicial, dataFinal)
                .Returns(transacoes);
            // Act
            var gastosPorCategoria = await _service.GetGastoPorCategoriaAsync(_ambienteId, dataInicial: dataInicial, dataFinal: dataFinal);
            
            // Assert
            Assert.Equal(3, gastosPorCategoria.Count());
            Assert.Equal(1000, gastosPorCategoria.Sum(g => g.TotalGasto));

            //Valida valores retornados para cada categoria, garantindo que apenas as transações dentro do período foram consideradas
            var gastosAlimentacao = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaAlimentacao.Id);
            Assert.Equal(20, gastosAlimentacao.Percentual);

            var gastosTransporte = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaTransporte.Id);
            Assert.Equal(30, gastosTransporte.Percentual);

            var gastosLazer = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaLazer.Id);
            Assert.Equal(50, gastosLazer.Percentual);

        }

        [Fact]
        public async Task GetGastosPorCategoriaAsync_RetornaPorMesEAno()
        {
            //Arrange
            var mes = 9;
            var ano = 2026;

            var CategoriaAlimentacao = new Categoria { Id = Guid.NewGuid(), Nome = "Alimentação" };
            var CategoriaTransporte = new Categoria { Id = Guid.NewGuid(), Nome = "Transporte" };
            var CategoriaLazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };

            var transacoes = new List<Transacao>
        {
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 200, CategoriaId = CategoriaAlimentacao.Id, Categoria = CategoriaAlimentacao },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, CategoriaId = CategoriaTransporte.Id, Categoria = CategoriaTransporte },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 500, CategoriaId = CategoriaLazer.Id, Categoria = CategoriaLazer },
        };
            _repository.TransacaoRepository
                .GetByMesCompetenciaAsync(_ambienteId, mes, ano)
                .Returns(transacoes);

            // Act
            var gastosPorCategoria = await _service.GetGastoPorCategoriaAsync(_ambienteId, mes, ano);

            // Assert
            Assert.Equal(3, gastosPorCategoria.Count());
            Assert.Equal(1000, gastosPorCategoria.Sum(g => g.TotalGasto));

            //Valida valores retornados para cada categoria, garantindo que apenas as transações dentro do período foram consideradas
            var gastosAlimentacao = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaAlimentacao.Id);
            Assert.Equal(20, gastosAlimentacao.Percentual);

            var gastosTransporte = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaTransporte.Id);
            Assert.Equal(30, gastosTransporte.Percentual);

            var gastosLazer = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaLazer.Id);
            Assert.Equal(50, gastosLazer.Percentual);

        }


        [Fact]
        public async Task GetGastosPorCategoriaAsync_ComTransferencia_ConsideraApenasDespesas()
        {
            //Arrange
            var mes = 9;
            var ano = 2026;

            var CategoriaAlimentacao = new Categoria { Id = Guid.NewGuid(), Nome = "Alimentação" };
            var CategoriaTransporte = new Categoria { Id = Guid.NewGuid(), Nome = "Transporte" };
            var CategoriaLazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };

            var transacoes = new List<Transacao>
        {
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 200, CategoriaId = CategoriaAlimentacao.Id, Categoria = CategoriaAlimentacao },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, CategoriaId = CategoriaTransporte.Id, Categoria = CategoriaTransporte },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 500, CategoriaId = CategoriaLazer.Id, Categoria = CategoriaLazer },
            new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 500, CategoriaId = CategoriaLazer.Id, Categoria = CategoriaLazer },
        };
            _repository.TransacaoRepository
                .GetByMesCompetenciaAsync(_ambienteId, mes, ano)
                .Returns(transacoes);

            // Act
            var gastosPorCategoria = await _service.GetGastoPorCategoriaAsync(_ambienteId, mes, ano);

            // Assert
            Assert.Equal(3, gastosPorCategoria.Count());
            Assert.Equal(1000, gastosPorCategoria.Sum(g => g.TotalGasto));

            //Valida valores retornados para cada categoria, garantindo que apenas as transações dentro do período foram consideradas
            var gastosAlimentacao = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaAlimentacao.Id);
            Assert.Equal(20, gastosAlimentacao.Percentual);

            var gastosTransporte = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaTransporte.Id);
            Assert.Equal(30, gastosTransporte.Percentual);

            var gastosLazer = Assert.Single(gastosPorCategoria, g => g.CategoriaId == CategoriaLazer.Id);
            Assert.Equal(50, gastosLazer.Percentual);

        }

        [Fact]
        public async Task GetGastosPorCategoriaAsync_OrdenacaoMaiorMenor()
        {
            //Arrange
            DateTime dataInicial = new DateTime(2026, 9, 1);
            DateTime dataFinal = new DateTime(2026, 9, 30);

            var CategoriaAlimentacao = new Categoria { Id = Guid.NewGuid(), Nome = "Alimentação" };
            var CategoriaTransporte = new Categoria { Id = Guid.NewGuid(), Nome = "Transporte" };
            var CategoriaLazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };

            var transacoes = new List<Transacao>
        {
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 200, CategoriaId = CategoriaAlimentacao.Id, Categoria = CategoriaAlimentacao },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, CategoriaId = CategoriaTransporte.Id, Categoria = CategoriaTransporte },
            new() { TipoTransacao = TipoTransacao.Despesa, Valor = 500, CategoriaId = CategoriaLazer.Id, Categoria = CategoriaLazer },
        };
            _repository.TransacaoRepository
                .GetByPeriodoAsync(_ambienteId, dataInicial, dataFinal)
                .Returns(transacoes);
            // Act
            var gastosPorCategoria = await _service.GetGastoPorCategoriaAsync(_ambienteId, dataInicial: dataInicial, dataFinal: dataFinal);

            // Assert
            Assert.Equal(gastosPorCategoria.ElementAt(0).CategoriaId, CategoriaLazer.Id);
            Assert.Equal(gastosPorCategoria.ElementAt(1).CategoriaId, CategoriaTransporte.Id);
            Assert.Equal(gastosPorCategoria.ElementAt(2).CategoriaId, CategoriaAlimentacao.Id);

        }

        #endregion
    }
}
