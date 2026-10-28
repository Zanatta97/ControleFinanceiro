using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using ControleFinanceiroAPI.Tests.Helpers;
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

        [Fact]
        public async Task GetProjecaoProximoMesAsync_ComCategoriasFixasERecebimento_EstimaPelaMediaDoHistorico()
        {
            // Arrange — alvo 10/2026; histórico 07 a 09/2026
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var aluguel = new Categoria { Id = Guid.NewGuid(), Nome = "Aluguel" };
            var salario = new Categoria { Id = Guid.NewGuid(), Nome = "Salário" };

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 1500, corrente, aluguel, new DateOnly(2026, 7, 1)),
                CriarLancamento(TipoTransacao.Despesa, 1500, corrente, aluguel, new DateOnly(2026, 8, 1)),
                CriarLancamento(TipoTransacao.Despesa, 1500, corrente, aluguel, new DateOnly(2026, 9, 1)),
                CriarLancamento(TipoTransacao.Receita, 5000, corrente, salario, new DateOnly(2026, 7, 1)),
                CriarLancamento(TipoTransacao.Receita, 5000, corrente, salario, new DateOnly(2026, 8, 1)),
                CriarLancamento(TipoTransacao.Receita, 1000, corrente, salario, new DateOnly(2026, 10, 1)), // já lançado no alvo
            };

            ConfigurarProjecaoProximoMes(new DateOnly(2026, 10, 1), [corrente], [aluguel, salario], transacoes,
                new SaldoMensalConta { ContaId = corrente.Id, SaldoInicial = 2000 });

            // Act
            var projecao = await _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, [aluguel.Id], [salario.Id]);

            // Assert
            var conta = Assert.Single(projecao.Contas);
            Assert.Equal(500, conta.SaldoInicialPrevisto);        // 2000 salvo − 1500 do mês base
            Assert.Equal(1000, conta.EntradasLancadas);
            Assert.Equal(0, conta.SaidasLancadas);
            Assert.Equal(1500, conta.FixosEstimados);             // média 1500 − 0 lançado
            Assert.Equal(2333.33m, conta.RecebimentosEstimados);  // média 3333,33 − 1000 lançado
            Assert.Equal(2333.33m, conta.SaldoFinalPrevisto);
            Assert.Null(conta.FaturaPrevista);                    // só cartão tem fatura

            Assert.Equal(10, projecao.Mes);
            Assert.Equal(2026, projecao.Ano);
            Assert.Equal(3333.33m, projecao.ReceitasPrevistas);
            Assert.Equal(1500, projecao.DespesasPrevistas);
            Assert.Equal(1833.33m, projecao.ResultadoPrevisto);
            Assert.Equal(2333.33m, projecao.SaldoPrevisto);
        }

        [Fact]
        public async Task GetProjecaoProximoMesAsync_LancadoAcimaDaMedia_NaoEstimaValorNegativo()
        {
            // Arrange — aluguel já lançado no alvo acima da média
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var aluguel = new Categoria { Id = Guid.NewGuid(), Nome = "Aluguel" };
            var salario = new Categoria { Id = Guid.NewGuid(), Nome = "Salário" };

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 1500, corrente, aluguel, new DateOnly(2026, 9, 1)),
                CriarLancamento(TipoTransacao.Despesa, 2000, corrente, aluguel, new DateOnly(2026, 10, 1)),
            };

            ConfigurarProjecaoProximoMes(new DateOnly(2026, 10, 1), [corrente], [aluguel, salario], transacoes);

            // Act
            var projecao = await _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, [aluguel.Id], [salario.Id]);

            // Assert
            var conta = Assert.Single(projecao.Contas);
            Assert.Equal(0, conta.FixosEstimados);
            Assert.Equal(2000, conta.SaidasLancadas);
        }

        [Fact]
        public async Task GetProjecaoProximoMesAsync_SemMesEAno_UsaMesSeguinteADataDeReferencia()
        {
            // Arrange — referência em dezembro: alvo vira janeiro do ano seguinte
            ConfigurarProjecaoProximoMes(new DateOnly(2027, 1, 1), [], [], []);

            // Act
            var projecao = await _service.GetProjecaoProximoMesAsync(_ambienteId, dataReferencia: new DateTime(2026, 12, 15));

            // Assert
            Assert.Equal(1, projecao.Mes);
            Assert.Equal(2027, projecao.Ano);
            Assert.Empty(projecao.Contas);
        }

        [Fact]
        public async Task GetProjecaoProximoMesAsync_ComCategoriaNasDuasListas_LancaArgumentException()
        {
            // Arrange
            var categoria = Guid.NewGuid();

            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, [categoria], [categoria]));

            // Assert
            Assert.Equal("Uma mesma categoria não pode estar entre as fixas e as de recebimento.", excecao.Message);
        }

        [Fact]
        public async Task GetProjecaoProximoMesAsync_ComCategoriaDeOutroAmbiente_LancaArgumentException()
        {
            // Arrange
            var desconhecida = Guid.NewGuid();
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Categoria>());

            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, [desconhecida], []));

            // Assert
            Assert.Equal($"Categoria não encontrada no ambiente ativo: {desconhecida}.", excecao.Message);
        }

        [Theory]
        [InlineData(10, null)]
        [InlineData(null, 2026)]
        public async Task GetProjecaoProximoMesAsync_ComSoMesOuSoAno_LancaArgumentException(int? mes, int? ano)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetProjecaoProximoMesAsync(_ambienteId, mes, ano));

            // Assert
            Assert.Equal("Informe mês e ano juntos, ou nenhum dos dois.", excecao.Message);
        }

        // Prepara os repositórios lidos pela projeção do mês "alvo"
        private void ConfigurarProjecaoProximoMes(DateOnly alvo, List<Conta> contas, List<Categoria> categorias,
                                                  List<Transacao> transacoes, params SaldoMensalConta[] saldosMesBase)
        {
            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(contas);
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(categorias);
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, alvo.AddMonths(-1))
                .Returns(saldosMesBase.ToList());
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, alvo.AddMonths(-3), alvo.AddMonths(1).AddDays(-1))
                .Returns(transacoes);
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, alvo.AddMonths(-36))
                .Returns(transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList());
        }

        // Lançamento comum (sem parcela) na competência informada
        private Transacao CriarLancamento(TipoTransacao tipo, decimal valor, Conta? conta, Categoria? categoria,
                                          DateOnly competencia, DateTime? data = null, string? observacao = null)
        {
            return new Transacao
            {
                Id = Guid.NewGuid(),
                Descricao = categoria?.Nome ?? "Lançamento",
                TipoTransacao = tipo,
                Valor = valor,
                Data = data ?? competencia.ToDateTime(TimeOnly.MinValue),
                Observacao = observacao,
                ContaId = conta?.Id ?? Guid.NewGuid(),
                Conta = conta,
                CategoriaId = categoria?.Id ?? Guid.Empty,
                Categoria = categoria,
                AmbienteId = _ambienteId,
                MesCompetencia = competencia
            };
        }
        #endregion

        #region GetResumoAsync Complementares Tests
        [Fact]
        public async Task GetResumoAsync_SemTransacoes_RetornaZerosComMesEAno()
        {
            // Arrange
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 3, 2026).Returns(new List<Transacao>());

            // Act
            var resumo = await _service.GetResumoAsync(_ambienteId, 3, 2026);

            // Assert
            Assert.Equal(3, resumo.Mes);
            Assert.Equal(2026, resumo.Ano);
            Assert.Equal(0, resumo.TotalReceitas);
            Assert.Equal(0, resumo.TotalDespesas);
            Assert.Equal(0, resumo.Saldo);
        }

        [Fact]
        public async Task GetResumoAsync_DespesaSemContaCarregada_EntraEmDespesasOutras()
        {
            // Arrange
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Receita, Valor = 100 },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 40, Conta = null },
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var resumo = await _service.GetResumoAsync(_ambienteId, 9, 2026);

            // Assert
            Assert.Equal(100, resumo.TotalReceitas);
            Assert.Equal(0, resumo.DespesasCartao);
            Assert.Equal(40, resumo.DespesasOutras);
            Assert.Equal(60, resumo.Saldo);
        }
        #endregion

        #region GetGastoPorCategoriaAsync Complementares Tests
        [Fact]
        public async Task GetGastoPorCategoriaAsync_DespesaSemCategoria_AgrupaComoSemCategoria()
        {
            // Arrange
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 80, CategoriaId = Guid.Empty, Categoria = null },
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var gasto = Assert.Single(await _service.GetGastoPorCategoriaAsync(_ambienteId, 9, 2026));

            // Assert
            Assert.Equal("Sem categoria", gasto.NomeCategoria);
            Assert.Equal(100, gasto.Percentual);
        }

        [Fact]
        public async Task GetGastoPorCategoriaAsync_TotalZero_PercentualZeroSemDividirPorZero()
        {
            // Arrange
            var categoria = new Categoria { Id = Guid.NewGuid(), Nome = "Grátis" };
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 0, CategoriaId = categoria.Id, Categoria = categoria },
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var gasto = Assert.Single(await _service.GetGastoPorCategoriaAsync(_ambienteId, 9, 2026));

            // Assert
            Assert.Equal(0, gasto.Percentual);
        }

        [Theory]
        [InlineData(9, 0)]
        [InlineData(0, 2026)]
        public async Task GetGastoPorCategoriaAsync_ComMesOuAnoIncompleto_LancaArgumentException(int mes, int ano)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetGastoPorCategoriaAsync(_ambienteId, mes, ano));

            // Assert
            Assert.Equal("Informe mês/ano ou período de datas.", excecao.Message);
        }

        [Fact]
        public async Task GetGastoPorCategoriaAsync_ComSoDataInicial_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetGastoPorCategoriaAsync(_ambienteId, dataInicial: new DateTime(2026, 9, 1)));

            // Assert
            Assert.Equal("Informe mês/ano ou período de datas.", excecao.Message);
        }
        #endregion

        #region GetEvolucaoMensalAsync Tests
        [Fact]
        public async Task GetEvolucaoMensalAsync_ComLancamentos_MontaDozeMesesPelaCompetencia()
        {
            // Arrange
            var corrente = new Conta { Id = Guid.NewGuid(), TipoConta = TipoConta.Corrente };
            var cartao = new Conta { Id = Guid.NewGuid(), TipoConta = TipoConta.CartaoCredito };

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Receita, 1000, corrente, null, new DateOnly(2026, 1, 1)),
                CriarLancamento(TipoTransacao.Despesa, 200, cartao, null, new DateOnly(2026, 1, 1)),
                CriarLancamento(TipoTransacao.Despesa, 300, corrente, null, new DateOnly(2026, 3, 1)),
                CriarLancamento(TipoTransacao.Transferencia, 500, corrente, null, new DateOnly(2026, 3, 1)),
                CriarLancamento(TipoTransacao.Despesa, 999, corrente, null, new DateOnly(2025, 12, 1)), // outro ano
            };
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
                .Returns(transacoes);

            // Act
            var evolucao = await _service.GetEvolucaoMensalAsync(_ambienteId, 2026);

            // Assert
            var meses = evolucao.Meses.ToList();
            Assert.Equal(2026, evolucao.Ano);
            Assert.Equal(12, meses.Count);
            Assert.Equal(Enumerable.Range(1, 12), meses.Select(m => m.Mes));

            Assert.Equal("janeiro", meses[0].NomeMes);
            Assert.Equal(1000, meses[0].TotalReceitas);
            Assert.Equal(200, meses[0].DespesasCartao);
            Assert.Equal(0, meses[0].DespesasOutras);
            Assert.Equal(200, meses[0].TotalDespesas);
            Assert.Equal(800, meses[0].Saldo);

            Assert.Equal(0, meses[1].TotalDespesas);

            Assert.Equal("março", meses[2].NomeMes);
            Assert.Equal(300, meses[2].DespesasOutras);
            Assert.Equal(-300, meses[2].Saldo);            // transferência não entra

            Assert.Equal(0, meses[11].TotalDespesas);      // dezembro de 2025 não conta
        }
        #endregion

        #region GetOrcamentosStatusAsync Tests
        [Fact]
        public async Task GetOrcamentosStatusAsync_ComDespesas_CalculaGastoAteADataLimiteEOrdenaPorPercentual()
        {
            // Arrange
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado" };
            var lazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };
            var limite = new DateTime(2026, 9, 30);

            var orcamentoMercado = new Orcamento { Id = Guid.NewGuid(), Nome = "Mercado", CategoriaId = mercado.Id, Categoria = mercado, ValorLimite = 1000, DataLimite = limite, StatusOrcamento = StatusOrcamento.Ativo };
            var orcamentoLazer = new Orcamento { Id = Guid.NewGuid(), Nome = "Lazer", CategoriaId = lazer.Id, Categoria = null, ValorLimite = 200, DataLimite = limite };
            var orcamentoSemLimite = new Orcamento { Id = Guid.NewGuid(), Nome = null, CategoriaId = mercado.Id, ValorLimite = 0, DataLimite = limite };

            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, CategoriaId = mercado.Id, Data = new DateTime(2026, 9, 10) },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 200, CategoriaId = mercado.Id, Data = new DateTime(2026, 10, 5) }, // depois do limite
                new() { TipoTransacao = TipoTransacao.Receita, Valor = 999, CategoriaId = mercado.Id, Data = new DateTime(2026, 9, 10) }, // receita não conta
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 150, CategoriaId = lazer.Id, Data = new DateTime(2026, 9, 1) },
            };

            _repository.OrcamentoRepository.GetAllByAmbienteAsync(_ambienteId)
                .Returns(new List<Orcamento> { orcamentoMercado, orcamentoLazer, orcamentoSemLimite });
            _repository.TransacaoRepository.GetAllByAmbienteAsync(_ambienteId).Returns(transacoes);

            // Act
            var status = (await _service.GetOrcamentosStatusAsync(_ambienteId)).ToList();

            // Assert — ordem crescente de percentual
            Assert.Equal([orcamentoSemLimite.Id, orcamentoMercado.Id, orcamentoLazer.Id], status.Select(s => s.Id));

            Assert.Equal(300, status[0].ValorGasto);
            Assert.Equal(0, status[0].Percentual);            // limite zero não divide
            Assert.Equal(string.Empty, status[0].NomeOrcamento);

            Assert.Equal(300, status[1].ValorGasto);
            Assert.Equal(30, status[1].Percentual);
            Assert.Equal("Mercado", status[1].NomeCategoria);
            Assert.Equal(StatusOrcamento.Ativo, status[1].Status);

            Assert.Equal(150, status[2].ValorGasto);
            Assert.Equal(75, status[2].Percentual);
            Assert.Equal("Sem categoria", status[2].NomeCategoria);
        }

        [Fact]
        public async Task GetOrcamentosStatusAsync_SemOrcamentos_RetornaVazio()
        {
            // Arrange
            _repository.OrcamentoRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Orcamento>());
            _repository.TransacaoRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Transacao>());

            // Act / Assert
            Assert.Empty(await _service.GetOrcamentosStatusAsync(_ambienteId));
        }
        #endregion

        #region GetExtratoContaAsync Tests
        [Fact]
        public async Task GetExtratoContaAsync_ContaDoAmbiente_SomaEntradasESaidasComTransferencias()
        {
            // Arrange
            var conta = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente, Saldo = 1234, AmbienteId = _ambienteId };
            var outra = Guid.NewGuid();
            _repository.ContaRepository.ComDados(conta);

            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Receita,       Valor = 1000, ContaId = conta.Id },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 200,  ContaId = conta.Id },
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 300,  ContaId = conta.Id, ContaDestinoId = outra },
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 50,   ContaId = outra,    ContaDestinoId = conta.Id },
            };
            _repository.TransacaoRepository.GetByContaAndPeriodoAsync(conta.Id, _ambienteId, inicio, fim).Returns(transacoes);

            // Act
            var extrato = await _service.GetExtratoContaAsync(_ambienteId, conta.Id, inicio, fim);

            // Assert
            Assert.Equal(conta.Id, extrato.ContaId);
            Assert.Equal("Corrente", extrato.NomeConta);
            Assert.Equal(TipoConta.Corrente, extrato.TipoConta);
            Assert.Equal(1234, extrato.SaldoAtual);
            Assert.Equal(inicio, extrato.DataInicio);
            Assert.Equal(fim, extrato.DataFim);
            Assert.Equal(1050, extrato.TotalEntradas);
            Assert.Equal(500, extrato.TotalSaidas);
            Assert.Equal(4, extrato.Transacoes.Count());
        }

        [Fact]
        public async Task GetExtratoContaAsync_ContaInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.ContaRepository.ComDados();

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetExtratoContaAsync(_ambienteId, Guid.NewGuid(), DateTime.Today, DateTime.Today));

            // Assert
            Assert.Equal("Conta não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task GetExtratoContaAsync_ContaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var alheia = new Conta { Id = Guid.NewGuid(), AmbienteId = Guid.NewGuid() };
            _repository.ContaRepository.ComDados(alheia);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetExtratoContaAsync(_ambienteId, alheia.Id, DateTime.Today, DateTime.Today));
            await _repository.TransacaoRepository.DidNotReceive()
                .GetByContaAndPeriodoAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }
        #endregion

        #region GetGastosPorContaAsync Tests
        [Fact]
        public async Task GetGastosPorContaAsync_ComDespesas_AgrupaPorContaComPercentualEOrdena()
        {
            // Arrange
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", TipoConta = TipoConta.CartaoCredito };
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 300, ContaId = corrente.Id, Conta = corrente },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 700, ContaId = cartao.Id,   Conta = cartao },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 100, ContaId = cartao.Id,   Conta = cartao },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 100, ContaId = Guid.NewGuid(), Conta = null },
                new() { TipoTransacao = TipoTransacao.Receita, Valor = 5000, ContaId = corrente.Id, Conta = corrente },
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var gastos = (await _service.GetGastosPorContaAsync(_ambienteId, 9, 2026)).ToList();

            // Assert
            Assert.Equal(["Cartão", "Corrente", "Sem conta"], gastos.Select(g => g.NomeConta));
            Assert.Equal(800, gastos[0].TotalGasto);
            Assert.Equal(66.67m, gastos[0].Percentual);
            Assert.Equal(TipoConta.CartaoCredito, gastos[0].TipoConta);
            Assert.Equal(25, gastos[1].Percentual);
            Assert.Equal(8.33m, gastos[2].Percentual);
            Assert.Null(gastos[2].TipoConta);
        }

        [Fact]
        public async Task GetGastosPorContaAsync_PorPeriodo_ConsultaPelaData()
        {
            // Arrange
            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            _repository.TransacaoRepository.GetByPeriodoAsync(_ambienteId, inicio, fim).Returns(new List<Transacao>());

            // Act
            var gastos = await _service.GetGastosPorContaAsync(_ambienteId, dataInicial: inicio, dataFinal: fim);

            // Assert
            Assert.Empty(gastos);
            await _repository.TransacaoRepository.Received(1).GetByPeriodoAsync(_ambienteId, inicio, fim);
        }

        [Fact]
        public async Task GetGastosPorContaAsync_ComMesAnoEPeriodo_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetGastosPorContaAsync(_ambienteId, 9, 2026, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30)));

            // Assert
            Assert.Equal("Informe apenas mês/ano ou período de datas, não ambos.", excecao.Message);
        }

        [Fact]
        public async Task GetGastosPorContaAsync_SemFiltro_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetGastosPorContaAsync(_ambienteId));

            // Assert
            Assert.Equal("Informe mês/ano ou período de datas.", excecao.Message);
        }
        #endregion

        #region GetComparativoCategoriaAsync Tests
        [Fact]
        public async Task GetComparativoCategoriaAsync_ComQuatroMeses_CalculaMediaEVariacoes()
        {
            // Arrange — M = 09/2026; média = 07, 08 e 06/2026
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado", Cor = "#111" };
            var lazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };
            var viagem = new Categoria { Id = Guid.NewGuid(), Nome = "Viagem" };

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 300, null, mercado, new DateOnly(2026, 9, 1)),
                CriarLancamento(TipoTransacao.Despesa, 200, null, mercado, new DateOnly(2026, 8, 1)),
                CriarLancamento(TipoTransacao.Despesa, 100, null, mercado, new DateOnly(2026, 7, 1)),
                CriarLancamento(TipoTransacao.Despesa, 90, null, lazer, new DateOnly(2026, 9, 1)),
                CriarLancamento(TipoTransacao.Despesa, 600, null, viagem, new DateOnly(2026, 6, 1)),
                CriarLancamento(TipoTransacao.Receita, 9999, null, mercado, new DateOnly(2026, 9, 1)),
            };
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 30))
                .Returns(transacoes);

            // Act
            var comparativo = (await _service.GetComparativoCategoriaAsync(_ambienteId, 9, 2026)).ToList();

            // Assert — ordem: gasto do mês desc, depois média desc
            Assert.Equal(["Mercado", "Lazer", "Viagem"], comparativo.Select(c => c.NomeCategoria));

            Assert.Equal(300, comparativo[0].GastoMes);
            Assert.Equal(200, comparativo[0].GastoMesAnterior);
            Assert.Equal(100, comparativo[0].MediaTresMesesAnteriores);
            Assert.Equal(50, comparativo[0].VariacaoMesAnterior);
            Assert.Equal(200, comparativo[0].VariacaoMedia);
            Assert.Equal("#111", comparativo[0].Cor);

            Assert.Null(comparativo[1].VariacaoMesAnterior);   // base zero: sem variação
            Assert.Null(comparativo[1].VariacaoMedia);

            Assert.Equal(0, comparativo[2].GastoMes);
            Assert.Equal(200, comparativo[2].MediaTresMesesAnteriores);
            Assert.Equal(-100, comparativo[2].VariacaoMedia);
        }

        [Fact]
        public async Task GetComparativoCategoriaAsync_EmJaneiro_ConsultaDesdeOutubroDoAnoAnterior()
        {
            // Arrange
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>())
                .Returns(new List<Transacao>());

            // Act
            await _service.GetComparativoCategoriaAsync(_ambienteId, 1, 2026);

            // Assert
            await _repository.TransacaoRepository.Received(1)
                .GetByIntervaloCompetenciaAsync(_ambienteId, new DateOnly(2025, 10, 1), new DateOnly(2026, 1, 31));
        }

        [Theory]
        [InlineData(0, 2026, "Informe um mês entre 1 e 12.")]
        [InlineData(13, 2026, "Informe um mês entre 1 e 12.")]
        [InlineData(9, 1899, "Informe um ano entre 1900 e 2100.")]
        [InlineData(9, 2101, "Informe um ano entre 1900 e 2100.")]
        public async Task GetComparativoCategoriaAsync_ComCompetenciaInvalida_LancaArgumentException(int mes, int ano, string mensagem)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetComparativoCategoriaAsync(_ambienteId, mes, ano));

            // Assert
            Assert.Equal(mensagem, excecao.Message);
            await _repository.TransacaoRepository.DidNotReceive()
                .GetByIntervaloCompetenciaAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>());
        }
        #endregion

        #region GetMaioresDespesasAsync Tests
        [Fact]
        public async Task GetMaioresDespesasAsync_ComQuantidade_RetornaMaioresOrdenadasPorValorEData()
        {
            // Arrange
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado", Cor = "#0f0" };
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão" };
            var competencia = new DateOnly(2026, 9, 1);

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 50, cartao, mercado, competencia, new DateTime(2026, 9, 1)),
                CriarLancamento(TipoTransacao.Despesa, 300, cartao, mercado, competencia, new DateTime(2026, 9, 2)),
                CriarLancamento(TipoTransacao.Despesa, 300, null, null, competencia, new DateTime(2026, 9, 5)),
                CriarLancamento(TipoTransacao.Despesa, 10, cartao, mercado, competencia, new DateTime(2026, 9, 6)),
                CriarLancamento(TipoTransacao.Receita, 1000, cartao, mercado, competencia, new DateTime(2026, 9, 6)),
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var maiores = (await _service.GetMaioresDespesasAsync(_ambienteId, 3, 9, 2026)).ToList();

            // Assert — empate no valor: mais recente primeiro
            Assert.Equal(3, maiores.Count);
            Assert.Equal([300m, 300m, 50m], maiores.Select(m => m.Valor));
            Assert.Equal(new DateTime(2026, 9, 5), maiores[0].Data);
            Assert.Equal("Sem categoria", maiores[0].NomeCategoria);
            Assert.Equal("Sem conta", maiores[0].NomeConta);
            Assert.Equal("Mercado", maiores[1].NomeCategoria);
            Assert.Equal("#0f0", maiores[1].Cor);
            Assert.Equal("Cartão", maiores[1].NomeConta);
            Assert.Equal(transacoes[1].Id, maiores[1].TransacaoId);
            Assert.Equal(competencia, maiores[1].MesCompetencia);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task GetMaioresDespesasAsync_ComQuantidadeForaDoLimite_LancaArgumentException(int quantidade)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetMaioresDespesasAsync(_ambienteId, quantidade, 9, 2026));

            // Assert
            Assert.Equal("A quantidade deve estar entre 1 e 100.", excecao.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task GetMaioresDespesasAsync_ComQuantidadeNoLimite_Aceita(int quantidade)
        {
            // Arrange
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(new List<Transacao>());

            // Act
            var maiores = await _service.GetMaioresDespesasAsync(_ambienteId, quantidade, 9, 2026);

            // Assert
            Assert.Empty(maiores);
        }
        #endregion

        #region GetMatrizCategoriaMesAsync Tests
        [Fact]
        public async Task GetMatrizCategoriaMesAsync_ComDespesas_MontaLinhasPorCategoriaETotaisPorMes()
        {
            // Arrange
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado" };
            var lazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };
            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 100, null, mercado, new DateOnly(2026, 1, 1)),
                CriarLancamento(TipoTransacao.Despesa, 50, null, mercado, new DateOnly(2026, 3, 1)),
                CriarLancamento(TipoTransacao.Despesa, 500, null, lazer, new DateOnly(2026, 3, 1)),
                CriarLancamento(TipoTransacao.Receita, 999, null, mercado, new DateOnly(2026, 3, 1)),
                CriarLancamento(TipoTransacao.Transferencia, 999, null, null, new DateOnly(2026, 3, 1)),
            };
            _repository.TransacaoRepository
                .GetByIntervaloCompetenciaAsync(_ambienteId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
                .Returns(transacoes);

            // Act
            var matriz = await _service.GetMatrizCategoriaMesAsync(_ambienteId, 2026);

            // Assert
            var categorias = matriz.Categorias.ToList();
            Assert.Equal(2026, matriz.Ano);
            Assert.Equal(["Lazer", "Mercado"], categorias.Select(c => c.NomeCategoria));

            var linhaMercado = categorias[1].Meses.ToList();
            Assert.Equal(12, linhaMercado.Count);
            Assert.Equal(100, linhaMercado[0].Valor);
            Assert.Equal(0, linhaMercado[1].Valor);
            Assert.Equal(50, linhaMercado[2].Valor);
            Assert.Equal("janeiro", linhaMercado[0].NomeMes);
            Assert.Equal(150, categorias[1].TotalAno);

            var totais = matriz.TotaisPorMes.ToList();
            Assert.Equal(12, totais.Count);
            Assert.Equal(100, totais[0].Valor);
            Assert.Equal(550, totais[2].Valor);
            Assert.Equal(650, matriz.TotalAno);
        }

        [Theory]
        [InlineData(1899)]
        [InlineData(2101)]
        public async Task GetMatrizCategoriaMesAsync_ComAnoInvalido_LancaArgumentException(int ano)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMatrizCategoriaMesAsync(_ambienteId, ano));

            // Assert
            Assert.Equal("Informe um ano entre 1900 e 2100.", excecao.Message);
        }
        #endregion

        #region GetFrequenciaTicketMedioAsync Tests
        [Fact]
        public async Task GetFrequenciaTicketMedioAsync_ComDespesas_CalculaQuantidadeETicketMedio()
        {
            // Arrange
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado" };
            var farmacia = new Categoria { Id = Guid.NewGuid(), Nome = "Farmácia" };
            var lazer = new Categoria { Id = Guid.NewGuid(), Nome = "Lazer" };
            var competencia = new DateOnly(2026, 9, 1);

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 10, null, mercado, competencia),
                CriarLancamento(TipoTransacao.Despesa, 20, null, mercado, competencia),
                CriarLancamento(TipoTransacao.Despesa, 30.33m, null, mercado, competencia),
                CriarLancamento(TipoTransacao.Despesa, 1, null, farmacia, competencia),
                CriarLancamento(TipoTransacao.Despesa, 1, null, farmacia, competencia),
                CriarLancamento(TipoTransacao.Despesa, 1, null, farmacia, competencia),
                CriarLancamento(TipoTransacao.Despesa, 500, null, lazer, competencia),
                CriarLancamento(TipoTransacao.Receita, 999, null, lazer, competencia),
            };
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var frequencia = (await _service.GetFrequenciaTicketMedioAsync(_ambienteId, 9, 2026)).ToList();

            // Assert — quantidade desc, depois total desc
            Assert.Equal(["Mercado", "Farmácia", "Lazer"], frequencia.Select(f => f.NomeCategoria));
            Assert.Equal(3, frequencia[0].Quantidade);
            Assert.Equal(60.33m, frequencia[0].TotalGasto);
            Assert.Equal(20.11m, frequencia[0].TicketMedio);
            Assert.Equal(1, frequencia[2].Quantidade);
            Assert.Equal(500, frequencia[2].TicketMedio);
        }

        [Fact]
        public async Task GetFrequenciaTicketMedioAsync_SemFiltro_LancaArgumentException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetFrequenciaTicketMedioAsync(_ambienteId));
        }
        #endregion

        #region GetRitmoMesAsync Tests
        [Fact]
        public async Task GetRitmoMesAsync_NoDia31ComFevereiroCurto_ComparaAteOUltimoDiaDeFevereiro()
        {
            // Arrange — referência 31/03/2026; fevereiro/2026 tem 28 dias
            var fimDoDia = new DateTime(2026, 3, 31).AddDays(1).AddTicks(-1);
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 100, Data = new DateTime(2026, 2, 10) },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 50,  Data = new DateTime(2026, 2, 28, 20, 0, 0) },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 200, Data = new DateTime(2026, 3, 5) },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 100, Data = new DateTime(2026, 3, 31, 22, 0, 0) },
                new() { TipoTransacao = TipoTransacao.Receita,       Valor = 999, Data = new DateTime(2026, 3, 15) },
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 77,  Data = new DateTime(2026, 3, 10) },
            };
            _repository.TransacaoRepository.GetByPeriodoAsync(_ambienteId, new DateTime(2026, 2, 1), fimDoDia).Returns(transacoes);

            // Act
            var ritmo = await _service.GetRitmoMesAsync(_ambienteId, new DateTime(2026, 3, 31, 15, 0, 0));

            // Assert
            Assert.Equal(new DateTime(2026, 3, 31), ritmo.DataReferencia);
            Assert.Equal(31, ritmo.DiasDecorridos);
            Assert.Equal(31, ritmo.DiasNoMes);
            Assert.Equal(28, ritmo.DiaComparadoMesAnterior);
            Assert.Equal(300, ritmo.GastoAteHoje);
            Assert.Equal(150, ritmo.GastoMesAnteriorAteMesmoDia);
            Assert.Equal(150, ritmo.GastoTotalMesAnterior);
            Assert.Equal(100, ritmo.VariacaoPercentual);
            Assert.Equal(300, ritmo.ProjecaoFimMes);
        }

        [Fact]
        public async Task GetRitmoMesAsync_NoMeioDoMes_ProjetaPeloRitmoEIgnoraRestoDoMesAnterior()
        {
            // Arrange — referência 10/09/2026
            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 100, Data = new DateTime(2026, 9, 3) },
                new() { TipoTransacao = TipoTransacao.Despesa, Valor = 400, Data = new DateTime(2026, 8, 20) }, // depois do dia 10
            };
            _repository.TransacaoRepository.GetByPeriodoAsync(_ambienteId, Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(transacoes);

            // Act
            var ritmo = await _service.GetRitmoMesAsync(_ambienteId, new DateTime(2026, 9, 10));

            // Assert
            Assert.Equal(10, ritmo.DiasDecorridos);
            Assert.Equal(30, ritmo.DiasNoMes);
            Assert.Equal(10, ritmo.DiaComparadoMesAnterior);
            Assert.Equal(100, ritmo.GastoAteHoje);
            Assert.Equal(0, ritmo.GastoMesAnteriorAteMesmoDia);
            Assert.Equal(400, ritmo.GastoTotalMesAnterior);
            Assert.Null(ritmo.VariacaoPercentual);       // base zero
            Assert.Equal(300, ritmo.ProjecaoFimMes);     // 100 / 10 dias × 30
        }
        #endregion

        #region GetFaturaCompetenciaAsync Tests
        [Fact]
        public async Task GetFaturaCompetenciaAsync_CartaoComDespesasEPagamento_CalculaSaldoEmAberto()
        {
            // Arrange
            var cartao = new Conta { Id = Guid.NewGuid(), Nome = "Nubank", TipoConta = TipoConta.CartaoCredito, AmbienteId = _ambienteId };
            var corrente = Guid.NewGuid();
            _repository.ContaRepository.ComDados(cartao);

            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 400, ContaId = cartao.Id },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 100, ContaId = cartao.Id },
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 300, ContaId = corrente,  ContaDestinoId = cartao.Id }, // pagamento
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 50,  ContaId = cartao.Id, ContaDestinoId = corrente },  // não é pagamento
                new() { TipoTransacao = TipoTransacao.Receita,       Valor = 20,  ContaId = cartao.Id },
            };
            _repository.TransacaoRepository.GetByMesCompetenciaEContaAsync(_ambienteId, cartao.Id, 9, 2026).Returns(transacoes);

            // Act
            var fatura = await _service.GetFaturaCompetenciaAsync(_ambienteId, cartao.Id, 9, 2026);

            // Assert
            Assert.Equal(cartao.Id, fatura.ContaId);
            Assert.Equal("Nubank", fatura.NomeConta);
            Assert.Equal(9, fatura.Mes);
            Assert.Equal(2026, fatura.Ano);
            Assert.Equal(500, fatura.TotalDespesas);
            Assert.Equal(300, fatura.TotalPago);
            Assert.Equal(200, fatura.SaldoEmAberto);
        }

        [Fact]
        public async Task GetFaturaCompetenciaAsync_ContaQueNaoECartao_LancaArgumentException()
        {
            // Arrange
            var corrente = new Conta { Id = Guid.NewGuid(), TipoConta = TipoConta.Corrente, AmbienteId = _ambienteId };
            _repository.ContaRepository.ComDados(corrente);

            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetFaturaCompetenciaAsync(_ambienteId, corrente.Id, 9, 2026));

            // Assert
            Assert.Equal("A conta informada não é um cartão de crédito.", excecao.Message);
        }

        [Fact]
        public async Task GetFaturaCompetenciaAsync_ContaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var alheio = new Conta { Id = Guid.NewGuid(), TipoConta = TipoConta.CartaoCredito, AmbienteId = Guid.NewGuid() };
            _repository.ContaRepository.ComDados(alheio);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetFaturaCompetenciaAsync(_ambienteId, alheio.Id, 9, 2026));

            // Assert
            Assert.Equal("Conta não encontrada.", excecao.Message);
        }

        [Fact]
        public async Task GetFaturaCompetenciaAsync_ComMesInvalido_LancaArgumentExceptionAntesDeConsultar()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetFaturaCompetenciaAsync(_ambienteId, Guid.NewGuid(), 13, 2026));

            // Assert
            Assert.Equal("Informe um mês entre 1 e 12.", excecao.Message);
            await _repository.ContaRepository.DidNotReceive()
                .GetByIdReadOnlyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Conta, bool>>>());
        }
        #endregion

        #region GetProjecaoParcelasAsync Complementares Tests
        [Fact]
        public async Task GetProjecaoParcelasAsync_DuasCompras_CalculaReducaoEParcelasTerminando()
        {
            // Arrange — A: 3×100 (09 a 11/2026); B: 2×50 (09 e 10/2026), todas lançadas
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente };
            var data = new DateTime(2026, 9, 5);
            var transacoes = new List<Transacao>
            {
                CriarParcela("A", corrente, data, 1, 3, new DateOnly(2026, 9, 1), 100),
                CriarParcela("A", corrente, data, 2, 3, new DateOnly(2026, 10, 1), 100),
                CriarParcela("A", corrente, data, 3, 3, new DateOnly(2026, 11, 1), 100),
                CriarParcela("B", corrente, data, 1, 2, new DateOnly(2026, 9, 1), 50),
                CriarParcela("B", corrente, data, 2, 2, new DateOnly(2026, 10, 1), 50),
            };
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(transacoes);

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 3);

            // Assert
            var meses = projecao.Meses.ToList();
            Assert.Equal(3, projecao.QuantidadeMeses);
            Assert.Equal(3, meses.Count);
            Assert.Equal(150, projecao.TotalParcelasMesReferencia);

            Assert.Equal((10, 2026), (meses[0].Mes, meses[0].Ano));
            Assert.Equal("outubro", meses[0].NomeMes);
            Assert.Equal(150, meses[0].TotalParcelas);
            Assert.Equal(0, meses[0].ReducaoEmRelacaoAoMesAnterior);
            Assert.Equal(50, meses[0].TotalParcelasTerminando);   // B 2/2

            Assert.Equal(100, meses[1].TotalParcelas);
            Assert.Equal(50, meses[1].ReducaoEmRelacaoAoMesAnterior);
            Assert.Equal(1, meses[1].QuantidadeParcelasTerminando); // A 3/3

            Assert.Equal(0, meses[2].TotalParcelas);
            Assert.Equal(100, meses[2].ReducaoEmRelacaoAoMesAnterior);

            // Ordem: última competência, depois descrição
            Assert.Equal(["B", "A"], projecao.ComprasAtivas.Select(c => c.Descricao));
            Assert.Equal(250, projecao.TotalRestante);
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_SemMesEAno_UsaMesDaDataDeReferencia()
        {
            // Arrange
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, Arg.Any<DateOnly>())
                .Returns(new List<Transacao>());

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, meses: 1, dataReferencia: new DateTime(2026, 5, 20));

            // Assert
            Assert.Equal(5, projecao.MesReferencia);
            Assert.Equal(2026, projecao.AnoReferencia);
            await _repository.TransacaoRepository.Received(1)
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 5, 1));
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_TransacaoSemParcela_NaoEntraNaProjecao()
        {
            // Arrange
            var corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente" };
            var comum = CriarParcela("Comum", corrente, new DateTime(2026, 9, 1), 1, 2, new DateOnly(2026, 9, 1), 100);
            comum.Observacao = "observação qualquer";
            _repository.TransacaoRepository
                .GetDespesasParceladasAPartirDeMesCompetenciaAsync(_ambienteId, new DateOnly(2023, 9, 1))
                .Returns(new List<Transacao> { comum });

            // Act
            var projecao = await _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 12);

            // Assert
            Assert.Empty(projecao.ComprasAtivas);
            Assert.Equal(0, projecao.TotalParcelasMesReferencia);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(37)]
        public async Task GetProjecaoParcelasAsync_ComMesesForaDoLimite_LancaArgumentException(int meses)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, meses));

            // Assert
            Assert.Equal("A quantidade de meses deve estar entre 1 e 36.", excecao.Message);
        }

        [Theory]
        [InlineData(9, null)]
        [InlineData(null, 2026)]
        public async Task GetProjecaoParcelasAsync_ComSoMesOuSoAno_LancaArgumentException(int? mes, int? ano)
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProjecaoParcelasAsync(_ambienteId, mes, ano));

            // Assert
            Assert.Equal("Informe mês e ano juntos, ou nenhum dos dois.", excecao.Message);
        }

        [Fact]
        public async Task GetProjecaoParcelasAsync_ComMesInvalido_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetProjecaoParcelasAsync(_ambienteId, 13, 2026));

            // Assert
            Assert.Equal("Informe um mês entre 1 e 12.", excecao.Message);
        }
        #endregion

        #region GetFixosRecebimentosAsync Tests
        [Fact]
        public async Task GetFixosRecebimentosAsync_ComLancamentos_SeparaFixosParcelasEVariaveis()
        {
            // Arrange
            var aluguel = new Categoria { Id = Guid.NewGuid(), Nome = "Aluguel", Cor = "#a00" };
            var salario = new Categoria { Id = Guid.NewGuid(), Nome = "Salário" };
            var freela = new Categoria { Id = Guid.NewGuid(), Nome = "Freela" };
            var mercado = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado" };
            var competencia = new DateOnly(2026, 9, 1);

            var transacoes = new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Receita, 5000, null, salario, competencia),
                CriarLancamento(TipoTransacao.Receita, 1000, null, freela, competencia),
                CriarLancamento(TipoTransacao.Despesa, 1500, null, aluguel, competencia),
                CriarLancamento(TipoTransacao.Despesa, 100, null, aluguel, competencia, observacao: "Parcela 1/2"), // fixa vence parcela
                CriarLancamento(TipoTransacao.Despesa, 200, null, mercado, competencia, observacao: "Parcela 2/5"),
                CriarLancamento(TipoTransacao.Despesa, 300, null, mercado, competencia),
                CriarLancamento(TipoTransacao.Transferencia, 999, null, null, competencia),
            };
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId)
                .Returns(new List<Categoria> { aluguel, salario, freela, mercado });
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(transacoes);

            // Act
            var relatorio = await _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, [aluguel.Id], [salario.Id]);

            // Assert
            Assert.Equal(9, relatorio.Mes);
            Assert.Equal(2026, relatorio.Ano);
            Assert.Equal(5000, relatorio.Recebimentos);
            Assert.Equal(1000, relatorio.OutrasReceitas);
            Assert.Equal(1600, relatorio.GastosFixos);
            Assert.Equal(200, relatorio.Parcelas);
            Assert.Equal(300, relatorio.GastosVariaveis);
            Assert.Equal(2100, relatorio.TotalDespesas);
            Assert.Equal(3900, relatorio.Saldo);
            Assert.Equal(36, relatorio.PercentualComprometido);
            Assert.Equal(32, relatorio.PercentualFixosSobreRecebimentos);

            var categorias = relatorio.Categorias.ToList();
            Assert.Equal(2, categorias.Count);
            Assert.Equal(("Aluguel", "Fixo", 1600m), (categorias[0].NomeCategoria, categorias[0].Grupo, categorias[0].Total));
            Assert.Equal("#a00", categorias[0].Cor);
            Assert.Equal(("Salário", "Recebimento", 5000m), (categorias[1].NomeCategoria, categorias[1].Grupo, categorias[1].Total));
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_SemRecebimentosNoMes_PercentuaisNulos()
        {
            // Arrange
            var aluguel = new Categoria { Id = Guid.NewGuid(), Nome = "Aluguel" };
            var salario = new Categoria { Id = Guid.NewGuid(), Nome = "Salário" };
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Categoria> { aluguel, salario });
            _repository.TransacaoRepository.GetByMesCompetenciaAsync(_ambienteId, 9, 2026).Returns(new List<Transacao>
            {
                CriarLancamento(TipoTransacao.Despesa, 1500, null, aluguel, new DateOnly(2026, 9, 1)),
            });

            // Act
            var relatorio = await _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, [aluguel.Id], [salario.Id]);

            // Assert
            Assert.Null(relatorio.PercentualComprometido);
            Assert.Null(relatorio.PercentualFixosSobreRecebimentos);
            Assert.Equal(-1500, relatorio.Saldo);
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_SemCategoriasFixas_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, null, [Guid.NewGuid()]));

            // Assert
            Assert.Equal("Informe ao menos uma categoria de gasto fixo.", excecao.Message);
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_SemCategoriasDeRecebimento_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, [Guid.NewGuid()], []));

            // Assert
            Assert.Equal("Informe ao menos uma categoria de recebimento.", excecao.Message);
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_ComCategoriaNasDuasListas_LancaArgumentException()
        {
            // Arrange
            var categoria = Guid.NewGuid();

            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, [categoria], [categoria]));

            // Assert
            Assert.Equal("Uma mesma categoria não pode estar entre as fixas e as de recebimento.", excecao.Message);
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_ComCategoriaForaDoAmbiente_LancaArgumentException()
        {
            // Arrange
            var conhecida = new Categoria { Id = Guid.NewGuid(), Nome = "Aluguel" };
            var desconhecida = Guid.NewGuid();
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Categoria> { conhecida });

            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, [conhecida.Id], [desconhecida]));

            // Assert
            Assert.Equal($"Categoria não encontrada no ambiente ativo: {desconhecida}.", excecao.Message);
        }

        [Fact]
        public async Task GetFixosRecebimentosAsync_ComMesInvalido_LancaArgumentException()
        {
            // Act
            var excecao = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetFixosRecebimentosAsync(_ambienteId, 0, 2026, [Guid.NewGuid()], [Guid.NewGuid()]));

            // Assert
            Assert.Equal("Informe um mês entre 1 e 12.", excecao.Message);
        }
        #endregion
    }
}
