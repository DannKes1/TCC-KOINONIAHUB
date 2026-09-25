using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    
    public sealed class ContextoDeTeste : IDisposable
    {
        private readonly SqliteConnection _conexao;

        public KoinoniaHubDbContext Db { get; }

        public ContextoDeTeste()
        {
            _conexao = new SqliteConnection("DataSource=:memory:");
            _conexao.Open();

            var opcoes = new DbContextOptionsBuilder<KoinoniaHubDbContext>()
                .UseSqlite(_conexao)
                .Options;

            Db = new KoinoniaHubDbContext(opcoes);
            Db.Database.EnsureCreated();
        }

     
        public KoinoniaHubDbContext NovoContexto()
        {
            var opcoes = new DbContextOptionsBuilder<KoinoniaHubDbContext>()
                .UseSqlite(_conexao)
                .Options;

            return new KoinoniaHubDbContext(opcoes);
        }

        public void Dispose()
        {
            Db.Dispose();
            _conexao.Dispose();
        }
    }
}
