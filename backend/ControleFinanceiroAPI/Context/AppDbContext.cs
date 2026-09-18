using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Context
{
    public class AppDbContext : IdentityDbContext<Usuario>
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }

        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Conta> Contas { get; set; }
        public DbSet<Orcamento> Orcamentos { get; set; }
        public DbSet<Transacao> Transacoes { get; set; }
        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<Ambiente> Ambientes { get; set; }
        public DbSet<AmbienteMembro> AmbienteMembros { get; set; }
        public DbSet<SaldoMensalConta> SaldoMensalContas { get; set; }

        //Não é necessário o DbSet para Usuario, pois ele é gerenciado pelo IdentityDbContext

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // AmbienteMembro não tem um Id próprio — a chave primária é a combinação dos dois campos
            builder.Entity<AmbienteMembro>()
                .HasKey(m => new { m.AmbienteId, m.UsuarioId }); // chave composta: um usuário só pode estar uma vez por ambiente

            // Relacionamento 1: Usuario → AmbienteAtivo
            // Um usuário tem no máximo um ambiente ativo por vez (nullable)
            builder.Entity<Usuario>()
                .HasOne(u => u.AmbienteAtivo)   // Usuario tem uma propriedade de navegação AmbienteAtivo
                .WithMany()                      // Ambiente não tem coleção de volta para "usuários que me têm como ativo"
                .HasForeignKey(u => u.AmbienteAtivoId) // a FK que fica na tabela Usuario é AmbienteAtivoId
                .OnDelete(DeleteBehavior.SetNull); // se o Ambiente for deletado, seta AmbienteAtivoId como null (não deleta o usuário)

            // Relacionamento 2: Ambiente → Usuario (criador)
            // Um ambiente foi criado por um único usuário
            builder.Entity<Ambiente>()
                .HasOne(a => a.Usuario)    // Ambiente tem uma propriedade de navegação Usuario (o criador)
                .WithMany()               // Usuario não tem coleção de volta para "ambientes que criei"
                .HasForeignKey(a => a.UsuarioId) // a FK que fica na tabela Ambiente é UsuarioId
                .OnDelete(DeleteBehavior.Restrict); // se tentar deletar o usuário criador, o banco bloqueia (evita órfãos)

            // Relacionamento 3: Transacao → Conta (origem) e Transacao → ContaDestino
            // Declarados explicitamente porque são duas navegações para a mesma entidade.
            builder.Entity<Transacao>()
                .HasOne(t => t.Conta)
                .WithMany()
                .HasForeignKey(t => t.ContaId)
                .OnDelete(DeleteBehavior.Cascade); // mantém o comportamento atual da conta de origem

            // Restrict evita "multiple cascade paths" no SQL Server (Conta já apaga Transacao via ContaId).
            // Na prática: uma conta que é destino de alguma transferência não pode ser excluída
            // enquanto essa transferência existir.
            builder.Entity<Transacao>()
                .HasOne(t => t.ContaDestino)
                .WithMany()
                .HasForeignKey(t => t.ContaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
