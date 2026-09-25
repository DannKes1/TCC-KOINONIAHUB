using System.Net;
using System.Net.Http.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // Definição de pronto da Etapa 0 (Plano, seção 5): a API sobe no .NET 10,
    // login e uma listagem funcionam, e o projeto de testes tem um teste verde.
    // Também confirma a decisão da seção 7.1: o modelo (índices únicos com filtro,
    // chaves estrangeiras) é aceito pelo SQLite em memória via EnsureCreated.
    public class Etapa0_FumacaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public Etapa0_FumacaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task RotaProtegida_SemCookie_Retorna401()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.GetAsync("/api/meus-dados");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task CadastroInicial_Login_EListagem_Funcionam()
        {
            var cliente = _fabrica.CreateClient();

            var cadastro = await cliente.PostAsJsonAsync("/api/auth/registrar-admin", new
            {
                Igreja = new { Nome = "Igreja de Teste" },
                EmailAdmin = "admin.fumaca@teste.com",
                SenhaAdmin = "Senha@123",
                NomeAdmin = "Administrador de Teste"
            });

            Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);
            Assert.True(cadastro.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies!, c => c.StartsWith("kh_token=", StringComparison.Ordinal));

            var login = await cliente.PostAsJsonAsync("/api/auth/login", new
            {
                Email = "admin.fumaca@teste.com",
                Senha = "Senha@123"
            });

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            // Listagem restrita a Admin/Pastor/Superintendente, autenticada pelo cookie.
            var pessoas = await cliente.GetAsync("/api/pessoas");

            Assert.Equal(HttpStatusCode.OK, pessoas.StatusCode);
        }

        [Fact]
        public async Task Login_ComSenhaErrada_Retorna400()
        {
            var cliente = _fabrica.CreateClient();

            var login = await cliente.PostAsJsonAsync("/api/auth/login", new
            {
                Email = "ninguem@teste.com",
                Senha = "senha-incorreta"
            });

            Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        }
    }
}
