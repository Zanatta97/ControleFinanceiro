using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;

namespace ControleFinanceiroAPI.Repositories
{
    /// <summary>
    /// Implementação de <see cref="IUnityOfWork"/>. Os repositórios são criados sob demanda no primeiro
    /// acesso e reaproveitados dentro da requisição, todos sobre o mesmo AppDbContext.
    /// </summary>
    public class UnityOfWork : IUnityOfWork
    {
        private readonly AppDbContext _context;

        private ICategoriaRepository? _categoriaRepository;
        private IContaRepository? _contaRepository;
        private ITransacaoRepository? _transacaoRepository;
        private IOrcamentoRepository? _trcamentoRepository;
        private IAmbienteRepository? _ambienteRepository;
        private ISaldoMensalRepository? _saldoMensalRepository;

        public UnityOfWork(AppDbContext context)
        {
            _context = context;
        }

        public ICategoriaRepository CategoriaRepository
        {
            get
            {
                return _categoriaRepository ??= new CategoriaRepository(_context);
            }
        }

        public IContaRepository ContaRepository
        {
            get
            {
                return _contaRepository ??= new ContaRepository(_context);
            }

        }

        public ITransacaoRepository TransacaoRepository
        {
            get
            {
                return _transacaoRepository ??= new TransacaoRepository(_context);
            }
        }

        public IOrcamentoRepository OrcamentoRepository
        {
            get
            {
                return _trcamentoRepository ??= new OrcamentoRepository(_context);
            }
        }

        public IAmbienteRepository AmbienteRepository
        {
            get
            {
                return _ambienteRepository ??= new AmbienteRepository(_context);
            }
        }

        public ISaldoMensalRepository SaldoMensalRepository
        {
            get
            {
                return _saldoMensalRepository ??= new SaldoMensalRepository(_context);
            }
        }

        /// <inheritdoc />
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
