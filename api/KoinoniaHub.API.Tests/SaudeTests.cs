using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // GET /api/saude (implantação, Entrega F): vivacidade para o healthcheck do contêiner,
    // pública (fora da FallbackPolicy e do gate do termo) e sem dado além de { situacao }.
    public class SaudeTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public SaudeTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Saude_SemSessao_Retorna200()
        {
            var resposta = await _fabrica.CreateClient().GetAsync("/api/saude");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("ok", json.RootElement.GetProperty("situacao").GetString());
            Assert.Single(json.RootElement.EnumerateObject());
        }
    }
}
