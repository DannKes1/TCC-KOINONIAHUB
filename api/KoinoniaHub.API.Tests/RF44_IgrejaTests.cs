using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF44 — Editar Dados da Igreja (RNFs 44.1, 44.2, 44.3, 44.5; CSU24).
    public class RF44_IgrejaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF44_IgrejaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Atualizar_ComoAdmin_PersisteERefleteNoTermo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/igrejas/{cenario.IgrejaId}", new
            {
                Nome = "  Igreja Renomeada RF44  ",
                Cidade = "Ji-Paraná",
                Estado = "ro",
                Email = "contato.rf44@igreja.teste",
                Telefone = "(69) 99999-0000"
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                var raiz = json.RootElement;
                Assert.Equal("Igreja Renomeada RF44", raiz.GetProperty("nome").GetString()); // Trim
                Assert.Equal("RO", raiz.GetProperty("estado").GetString());                   // UF em maiúsculas
                Assert.Equal("contato.rf44@igreja.teste", raiz.GetProperty("email").GetString());
                Assert.Equal("(69) 99999-0000", raiz.GetProperty("telefone").GetString());
                Assert.NotEqual(JsonValueKind.Null, raiz.GetProperty("atualizadoEm").ValueKind);
            }

            // Persistiu: o GET devolve os valores novos.
            var obter = await admin.GetAsync($"/api/igrejas/{cenario.IgrejaId}");
            Assert.Equal(HttpStatusCode.OK, obter.StatusCode);
            using (var json = JsonDocument.Parse(await obter.Content.ReadAsStringAsync()))
            {
                Assert.Equal("Igreja Renomeada RF44", json.RootElement.GetProperty("nome").GetString());
                Assert.Equal("(69) 99999-0000", json.RootElement.GetProperty("telefone").GetString());
            }

            // RNF 44.5 / 42.7: o cabeçalho do termo passa a mostrar o contato novo…
            var vigente = await admin.GetAsync("/api/termo/vigente");
            Assert.Equal(HttpStatusCode.OK, vigente.StatusCode);
            using (var json = JsonDocument.Parse(await vigente.Content.ReadAsStringAsync()))
            {
                var igreja = json.RootElement.GetProperty("igreja");
                Assert.Equal("Igreja Renomeada RF44", igreja.GetProperty("nome").GetString());
                Assert.Equal("contato.rf44@igreja.teste", igreja.GetProperty("email").GetString());
                Assert.Equal("(69) 99999-0000", igreja.GetProperty("telefone").GetString());
            }

            // …e o aceite já gravado continua válido (nada de termoPendente): o hash cobre só o texto.
            var meusDados = await admin.GetAsync("/api/meus-dados");
            Assert.Equal(HttpStatusCode.OK, meusDados.StatusCode);
            using (var json = JsonDocument.Parse(await meusDados.Content.ReadAsStringAsync()))
            {
                Assert.True(json.RootElement.GetProperty("aceiteTermo").GetProperty("vigente").GetBoolean());
            }
        }

        [Fact]
        public async Task Atualizar_ComoProfessor_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync($"/api/igrejas/{cenario.IgrejaId}", new { Nome = "Tentativa" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode); // RNF 44.1: só Admin

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            using var json = JsonDocument.Parse(await (await admin.GetAsync($"/api/igrejas/{cenario.IgrejaId}")).Content.ReadAsStringAsync());
            Assert.NotEqual("Tentativa", json.RootElement.GetProperty("nome").GetString());
        }

        [Fact]
        public async Task Atualizar_OutraIgreja_Retorna403()
        {
            var minha = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44c1");
            var outra = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44c2");
            var adminDaMinha = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, minha.EmailAdmin);

            var resposta = await adminDaMinha.PutAsJsonAsync($"/api/igrejas/{outra.IgrejaId}", new { Nome = "Invasão" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode); // RNF 44.2: isolamento por igreja

            var adminDaOutra = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, outra.EmailAdmin);
            using var json = JsonDocument.Parse(await (await adminDaOutra.GetAsync($"/api/igrejas/{outra.IgrejaId}")).Content.ReadAsStringAsync());
            Assert.NotEqual("Invasão", json.RootElement.GetProperty("nome").GetString());
        }

        [Fact]
        public async Task Atualizar_NomeVazio_Retorna400ComErroDoCampo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/igrejas/{cenario.IgrejaId}", new { Nome = "   ", Email = "ok@igreja.teste" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // RNF 44.3
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Nome", out _));
        }

        [Fact]
        public async Task Atualizar_EmailInvalido_Retorna400ComErroDoCampo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf44e");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/igrejas/{cenario.IgrejaId}", new { Nome = "Igreja", Email = "nao-e-email" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // RNF 44.3
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
        }
    }
}
