using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    public class RF2_OrigemTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string OrigemNaoAutorizada = "https://sitio-malicioso.exemplo";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF2_OrigemTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Post_ComOrigemNaoAutorizada_Retorna403()
        {
            var cliente = ClienteComOrigem(OrigemNaoAutorizada);

            var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = "a@b.com", Senha = "x" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("Origem não autorizada.", json.RootElement.GetProperty("mensagem").GetString());
        }

        [Fact]
        public async Task Post_ComOrigemAutorizada_Passa()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = "ninguem@teste.com", Senha = "senha-incorreta" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task Get_SemOrigem_Passa()
        {
            var cliente = ClienteComOrigem(null);

            var resposta = await cliente.GetAsync("/api/meus-dados");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Post_SemOrigem_Retorna403()
        {
            var cliente = ClienteComOrigem(null);

            var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = "a@b.com", Senha = "x" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Theory]
        [InlineData("PUT", "/api/pessoas/1")]
        [InlineData("PATCH", "/api/aulas/1/consolidar")]
        [InlineData("DELETE", "/api/departamentos/1/matriculas/1")]
        public async Task Escrita_ComOrigemNaoAutorizada_Retorna403AntesDaAutenticacao(string metodo, string rota)
        {
            var cliente = ClienteComOrigem(OrigemNaoAutorizada);
            using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), rota);

            var resposta = await cliente.SendAsync(requisicao);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Options_SemOrigem_NaoEhBloqueadoPeloMiddleware()
        {
            var cliente = ClienteComOrigem(null);
            using var requisicao = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");

            var resposta = await cliente.SendAsync(requisicao);

            Assert.NotEqual(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        private HttpClient ClienteComOrigem(string? origem)
        {
            var cliente = _fabrica.CreateClient();
            cliente.DefaultRequestHeaders.Remove("Origin");

            if (origem is not null)
                cliente.DefaultRequestHeaders.Add("Origin", origem);

            return cliente;
        }
    }
}
