using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF4 — Meus Dados (RNF 4.3): entrada inválida volta como 400 por campo e nada é gravado;
    // o caminho válido continua respondendo 204 com os dados refletidos no GET.
    public class RF4_MeusDadosTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF4_MeusDadosTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Atualizar_EmailInvalido_Retorna400ComErroDoCampo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { Email = "nao-e-email", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // RNF 4.3
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
            }

            // Nada gravado: a cidade enviada junto não entrou.
            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.NotEqual("Ji-Paraná", atual.RootElement.GetProperty("cidade").GetString());
        }

        [Fact]
        public async Task Atualizar_UfComTresLetras_Retorna400ComErroDoCampo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { Estado = "ROO", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode); // RNF 4.3: UF tem 2 letras
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Estado", out _));
            }

            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.NotEqual("Ji-Paraná", atual.RootElement.GetProperty("cidade").GetString());
        }

        [Fact]
        public async Task Atualizar_EmailVazio_EhOpcional_Retorna204EGravaNulo()
        {
            // O [EmailAddress] do .NET recusa a string vazia (só aceita nulo ou texto com um
            // único "@" fora das pontas); o DTO normaliza vazio/espaços para nulo antes da
            // validação, então o campo continua opcional.
            Assert.False(new EmailAddressAttribute().IsValid(""));
            Assert.True(new EmailAddressAttribute().IsValid(null));
            Assert.True(new EmailAddressAttribute().IsValid("a@b"));
            Assert.False(new EmailAddressAttribute().IsValid("a@b@c"));

            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4d");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { Email = "   ", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            using var json = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("email").ValueKind);
            Assert.Equal("Ji-Paraná", json.RootElement.GetProperty("cidade").GetString());
        }

        [Fact]
        public async Task Atualizar_DadosValidos_Retorna204EPersiste()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new
            {
                Celular = "(69) 99999-1234",
                Email = "professor.rf4c.novo@teste.com",
                Endereco = "Rua das Flores, 10",
                Bairro = "Centro",
                Cidade = "Ji-Paraná",
                Estado = "RO",
                CEP = "76900-000"
            });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

            using var json = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            var raiz = json.RootElement;
            Assert.Equal("(69) 99999-1234", raiz.GetProperty("celular").GetString());
            Assert.Equal("professor.rf4c.novo@teste.com", raiz.GetProperty("email").GetString());
            Assert.Equal("Ji-Paraná", raiz.GetProperty("cidade").GetString());
            Assert.Equal("RO", raiz.GetProperty("estado").GetString());
            Assert.Equal("76900-000", raiz.GetProperty("cep").GetString());
        }
    }
}
