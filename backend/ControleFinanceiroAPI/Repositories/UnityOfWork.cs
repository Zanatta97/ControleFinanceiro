using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces;

namespace ControleFinanceiroAPI.Repositories
{
    public class UnityOfWork : IUnityOfWork
    {
        private readonly AppDbContext _context;

        private ICategoriaRepository? _categoriaRepository;
        private IContaRepository? _contaRepository;
        private ITransacaoRepository? _transacaoRepository;
        private IOrcamentoRepository? _trcamentoRepository;

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

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
