using ControleFinanceiroAPI.DTO.SaldoMensal;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Services
{
    public class SaldoMensalService : ISaldoMensalService
    {
        private readonly IUnityOfWork _repository;

        public SaldoMensalService(IUnityOfWork repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SaldoMensalContaResponseDTO>> GetByMesAsync(Guid ambienteId, int mes, int ano)
        {
            var mes1 = new DateOnly(ano, mes, 1);
            var contas = await _repository.ContaRepository.GetAllByAmbienteAsync(ambienteId);
            var saldosSalvos = await _repository.SaldoMensalRepository.GetAllByAmbienteAsync(ambienteId, mes1);

            var resultado = new List<SaldoMensalContaResponseDTO>();

            foreach (var conta in contas)
            {
                var saldoSalvo = saldosSalvos.FirstOrDefault(s => s.ContaId == conta.Id);
                var transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaEContaAsync(ambienteId, conta.Id, mes, ano);
                var lista = transacoes.ToList();

                var entradas = lista.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor);
                var saidas = lista.Where(t => t.TipoTransacao == TipoTransacao.Despesa).Sum(t => t.Valor);
                var saldoInicial = saldoSalvo?.SaldoInicial ?? 0;
                var saldoFinal = saldoInicial + entradas - saidas;

                resultado.Add(new SaldoMensalContaResponseDTO
                {
                    ContaId = conta.Id,
                    NomeConta = conta.Nome,
                    TipoConta = conta.TipoConta,
                    SaldoInicial = saldoInicial,
                    TotalEntradas = entradas,
                    TotalSaidas = saidas,
                    SaldoFinal = saldoFinal,
                    Diferenca = saldoFinal - saldoInicial
                });
            }

            return resultado;
        }

        public async Task SalvarSaldoInicialAsync(Guid ambienteId, Guid contaId, int mes, int ano, decimal saldoInicial)
        {
            var mes1 = new DateOnly(ano, mes, 1);
            _repository.SaldoMensalRepository.AddOrUpdate(new SaldoMensalConta
            {
                ContaId = contaId,
                AmbienteId = ambienteId,
                Mes = mes1,
                SaldoInicial = saldoInicial
            });
            await _repository.SaveChangesAsync();
        }

        public async Task CalcularSaldoInicialAsync(Guid ambienteId, int mes, int ano)
        {
            var mesInicio = new DateOnly(ano, mes, 1);

            var (mesAnterior, anoAnterior) = mes == 1 ? (12, ano - 1) : (mes - 1, ano);

            var contas = await _repository.ContaRepository.GetAllByAmbienteAsync(ambienteId);
            var saldosMesAnterior = await _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(ambienteId, new DateOnly(anoAnterior, mesAnterior, 1));

            foreach (var conta in contas)
            {
                var saldoAnterior = saldosMesAnterior.FirstOrDefault(s => s.ContaId == conta.Id);
                var transacoes = await _repository.TransacaoRepository
                    .GetByMesCompetenciaEContaAsync(ambienteId, conta.Id, mesAnterior, anoAnterior);

                var lista = transacoes.ToList();
                var entradas = lista.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor);
                var saidas = lista.Where(t => t.TipoTransacao == TipoTransacao.Despesa).Sum(t => t.Valor);

                var saldoFinalAnterior = (saldoAnterior?.SaldoInicial ?? 0) + entradas - saidas;

                _repository.SaldoMensalRepository.AddOrUpdate(new SaldoMensalConta
                {
                    ContaId = conta.Id,
                    AmbienteId = ambienteId,
                    Mes = mesInicio,
                    SaldoInicial = saldoFinalAnterior
                });
            }

            await _repository.SaveChangesAsync();
        }
    }
}
