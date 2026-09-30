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
        #region GetProjecaoParcelasAsync Tests
        [Fact]
        public async Task GetProjecaoParcelasAsync_CompraLancadaSoAteReferencia_ProjetaParcelasRestantes()
        {
            // Arrange — compra 2/5 lançada só até a referência (09/2026), como na carga de faturas
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", TipoConta = TipoConta.CartaoCredito };
            var dataCompra = new DateTime(2026, 7, 12);

            var transacoes = new List<Transacao>
        {
            CriarParcela("Otica", cartao, dataCompra, 1, 5, new DateOnly(2026, 8, 1), 100),
            CriarParcela("Otica", cartao, dataCompra, 2, 5, new DateOnly(2026, 9, 1), 100),
        };

            // A busca recua 36 meses a partir da referência para achar a última parcela lançada
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 12);

            // Assert — parcelas 3, 4 e 5 projetadas em 10, 11 e 12/2026
            var meses = projecao.Meses.ToList();
            Assert.Equal(100, projecao.TotalParcelasMesReferencia);
            Assert.Equal(100, meses[0].TotalParcelas); // 10/2026
            Assert.Equal(1, meses[0].QuantidadeParcelas);
            Assert.Equal(100, meses[1].TotalParcelas); // 11/2026
            Assert.Equal(100, meses[2].TotalParcelas); // 12/2026
            Assert.Equal(1, meses[2].QuantidadeParcelasTerminando); // parcela 5/5
            Assert.Equal(0, meses[3].TotalParcelas); // 01/2027: compra já terminou
            Assert.Equal(300, projecao.TotalRestante);

            var compra = Assert.Single(projecao.ComprasAtivas);
            Assert.Equal(2, compra.ParcelaAtual);
            Assert.Equal(3, compra.ParcelasRestantes);
            Assert.Equal(3, compra.ParcelasProjetadas);
            Assert.Equal(300, compra.ValorRestante);
            Assert.Equal(dataCompra, compra.DataCompra);
            Assert.Equal(new DateOnly(2026, 12, 1), compra.UltimaCompetencia);
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_ParcelasImportadasComDataPorFatura_AgrupaNumaCompraSo()
        {
            // Arrange — na carga de faturas cada parcela traz a data da sua fatura
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", TipoConta = TipoConta.CartaoCredito };

            var transacoes = new List<Transacao>
        {
            CriarParcela("Otica", cartao, new DateTime(2026, 7, 12), 7, 10, new DateOnly(2026, 8, 1), 199.80m),
            CriarParcela("Otica", cartao, new DateTime(2026, 8, 12), 8, 10, new DateOnly(2026, 9, 1), 199.80m),
        };

            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 12);

            // Assert — uma compra só, com 9/10 e 10/10 projetadas (sem projetar duas vezes)
            var compra = Assert.Single(projecao.ComprasAtivas);
            Assert.Equal(8, compra.ParcelaAtual);
            Assert.Equal(2, compra.ParcelasRestantes);
            Assert.Equal(2, compra.ParcelasProjetadas);
            Assert.Equal(new DateTime(2026, 7, 12), compra.DataCompra); // data da parcela lançada de menor número
            Assert.Equal(399.60m, projecao.TotalRestante);
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_TodasParcelasLancadas_NaoProjetaNemDuplica()
        {
            // Arrange — compra 1/3 a 3/3 toda lançada, cadastrada pelo app (mesma data em todas)
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var dataCompra = new DateTime(2026, 9, 5);

            var transacoes = new List<Transacao>
        {
            CriarParcela("Geladeira", corrente, dataCompra, 1, 3, new DateOnly(2026, 9, 1), 100),
            CriarParcela("Geladeira", corrente, dataCompra, 2, 3, new DateOnly(2026, 10, 1), 100),
            CriarParcela("Geladeira", corrente, dataCompra, 3, 3, new DateOnly(2026, 11, 1), 100),
        };

            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 12);

            // Assert — cada mês tem só a parcela lançada
            var meses = projecao.Meses.ToList();
            Assert.Equal(1, meses[0].QuantidadeParcelas); // 10/2026
            Assert.Equal(100, meses[0].TotalParcelas);
            Assert.Equal(1, meses[1].QuantidadeParcelas); // 11/2026
            Assert.Equal(0, meses[2].QuantidadeParcelas); // 12/2026
            Assert.Equal(200, projecao.TotalRestante);

            var compra = Assert.Single(projecao.ComprasAtivas);
            Assert.Equal(2, compra.ParcelasRestantes);
            Assert.Equal(0, compra.ParcelasProjetadas);
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_CompraTerminadaAntesDaReferencia_NaoAparece()
        {
            // Arrange — última parcela (3/3) em 07/2026, antes da referência
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var dataCompra = new DateTime(2026, 5, 5);

            var transacoes = new List<Transacao>
        {
            CriarParcela("Sofá", corrente, dataCompra, 1, 3, new DateOnly(2026, 5, 1), 100),
            CriarParcela("Sofá", corrente, dataCompra, 2, 3, new DateOnly(2026, 6, 1), 100),
            CriarParcela("Sofá", corrente, dataCompra, 3, 3, new DateOnly(2026, 7, 1), 100),
        };

            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 12);

            // Assert
            Assert.Empty(projecao.ComprasAtivas);
            Assert.Equal(0, projecao.TotalRestante);
            Assert.Equal(0, projecao.TotalParcelasMesReferencia);
            Assert.All(projecao.Meses, m => Assert.Equal(0, m.QuantidadeParcelas));
        }

        // Parcela no formato gravado pelo cadastro: observação "Parcela N/M"
        private Transacao CriarParcela(string descricao, Conta conta, DateTime data, int numero, int total,
                                       DateOnly competencia, decimal valor)
        {
            return new Transacao
            {
                Id = Guid.NewGuid(),
                Descricao = descricao,
                Valor = valor,
                Data = data,
                Observacao = $"Parcela {numero}/{total}",
                TipoTransacao = TipoTransacao.Despesa,
                ContaId = conta.Id,
                Conta = conta,
                AmbienteId = _ambienteId,
                MesCompetencia = competencia
            };
        }
        #endregion
        #region GetProjecaoProximoMesAsync Tests
        [Fact]
        public async Task GetProjecaoProximoMesAsync_ParcelaProjetada_EntraNaFaturaSemContarLancadaDuasVezes()
        {
            // Arrange — alvo 10/2026, mês base 09/2026
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", TipoConta = TipoConta.CartaoCredito };

            // Compra A: só 2/3 lançada (09/2026) → 3/3 projetada no alvo
            // Compra B: 1/2 (09/2026) e 2/2 (10/2026) lançadas → nada projetado
            var transacoes = new List<Transacao>
        {
            CriarParcela("Compra A", cartao, new DateTime(2026, 8, 10), 2, 3, new DateOnly(2026, 9, 1), 100),
            CriarParcela("Compra B", cartao, new DateTime(2026, 9, 3), 1, 2, new DateOnly(2026, 9, 1), 50),
            CriarParcela("Compra B", cartao, new DateTime(2026, 9, 3), 2, 2, new DateOnly(2026, 10, 1), 50),
        };

            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Conta> { cartao });
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Categoria>());
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, new DateOnly(2026, 9, 1))
                .Returns(new List<SaldoMensalConta>());
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, new DateOnly(2026, 7, 1), new DateOnly(2026, 10, 31))
                .Returns(transacoes);
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 10, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026);

            // Assert — B 2/2 (50) é lançada; A 3/3 (100) é projetada
            var conta = Assert.Single(projecao.Contas);
            Assert.Equal(50, conta.SaidasLancadas);        // só a lançada
            Assert.Equal(100, conta.ParcelasProjetadas);   // só a projetada
            Assert.Equal(150, conta.FaturaPrevista);       // lançada + projetada, cada uma uma vez
            Assert.Equal(-300, conta.SaldoFinalPrevisto);  // −150 do mês base − 50 lançada − 100 projetada

            Assert.Equal(150, projecao.TotalParcelas);
            Assert.Equal(100, projecao.ParcelasProjetadas);
            Assert.Equal(150, projecao.DespesasPrevistas);
        }
        #endregion
    }
}
