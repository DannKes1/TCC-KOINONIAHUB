using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // Sobe a API completa em memória (Program.cs inteiro é executado) trocando
    // apenas o banco: em vez do PostgreSQL configurado no appsettings, usa um
    // SQLite em memória com a conexão mantida aberta durante toda a vida da
    // fábrica. O esquema é criado a partir do modelo (EnsureCreated), o que
    // preserva os índices únicos e as chaves estrangeiras que o provedor
    // InMemory do EF Core ignoraria (Plano de Desenvolvimento, seção 7.1).
    public class KoinoniaHubWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Chave só para testes: HS256 exige no mínimo 256 bits (32 caracteres).
        private const string ChaveJwtDeTeste =
            "koinoniahub-chave-de-teste-nao-usar-em-producao-0123456789abcdef";

        private SqliteConnection? _conexao;

        public KoinoniaHubWebApplicationFactory()
        {
            // O cookie kh_token é gravado com Secure = true; o CookieContainer
            // do HttpClient só o reenvia para endereços https.
            ClientOptions.BaseAddress = new Uri("https://localhost");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Valores lidos por Program.cs e por TokenServico durante a construção
            // do host; UseSetting os disponibiliza cedo o suficiente para isso.
            builder.UseSetting("Jwt:ChaveSecreta", ChaveJwtDeTeste);
            builder.UseSetting("Jwt:Emissor", "KoinoniaHub");
            builder.UseSetting("Jwt:Audiencia", "KoinoniaHub");
            builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=nao-usado-nos-testes");

            builder.ConfigureServices(services =>
            {
                // Remove a configuração UseNpgsql registrada em Program.cs e as
                // opções derivadas dela; AddDbContext abaixo as registra de novo
                // apontando para o SQLite.
                services.RemoveAll<IDbContextOptionsConfiguration<KoinoniaHubDbContext>>();
                services.RemoveAll<DbContextOptions<KoinoniaHubDbContext>>();
                services.RemoveAll<DbContextOptions>();

                var conexao = new SqliteConnection("DataSource=:memory:");
                conexao.Open();
                _conexao = conexao;

                services.AddDbContext<KoinoniaHubDbContext>(opcoes => opcoes.UseSqlite(conexao));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var escopo = host.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            db.Database.EnsureCreated();

            return host;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _conexao?.Dispose();
                _conexao = null;
            }
        }
    }
}
