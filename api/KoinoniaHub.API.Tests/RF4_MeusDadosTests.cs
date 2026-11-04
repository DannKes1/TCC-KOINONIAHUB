using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Regras;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF4 — Meus Dados (RNF 4.3): entrada inválida volta como 400 por campo e nada é gravado;
    // o caminho válido continua respondendo 204 com os dados refletidos no GET. Corretiva G2:
    // quem tem conta (sempre, aqui) não altera o próprio e-mail — ele é o e-mail de acesso.
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

        // Corretiva G4: toda mensagem de validacao dos DTOs e em portugues, no padrao
        // "O campo {nome} deve ter no maximo {N} caracteres." (nenhuma mensagem padrao do .NET).
        [Fact]
        public async Task Atualizar_CepCom60Caracteres_Retorna400ComMensagemEmPortugues()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4g4");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { CEP = new string('7', 60), Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                // G4b: 60 caracteres ferem o tamanho e o formato; as duas mensagens em português.
                var mensagens = json.RootElement.GetProperty("errors").GetProperty("CEP").EnumerateArray().Select(e => e.GetString()).ToList();
                Assert.Contains("O campo CEP deve ter no máximo 10 caracteres.", mensagens);
                Assert.Contains(Cep.Mensagem, mensagens);
            }

            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.NotEqual("Ji-Paraná", atual.RootElement.GetProperty("cidade").GetString());
        }

        // Corretiva G4b: CEP com 8 dígitos, com ou sem hífen; outro formato volta 400 em
        // errors.CEP com a mensagem do formato e nada é gravado.
        [Fact]
        public async Task Atualizar_CepComDezDigitos_Retorna400ComMensagemDoFormato()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4g4b1");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { CEP = "7777777777", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                var mensagens = json.RootElement.GetProperty("errors").GetProperty("CEP").EnumerateArray().Select(e => e.GetString()).ToList();
                Assert.Equal("Informe o CEP no formato 00000-000.", Assert.Single(mensagens));
            }

            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.NotEqual("Ji-Paraná", atual.RootElement.GetProperty("cidade").GetString());
        }

        // Corretiva G4b: CEP sem hífen é aceito e gravado como 00000-000.
        [Fact]
        public async Task Atualizar_CepSemHifen_Retorna204EGravaComHifen()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4g4b2");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { CEP = "76900000" });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.Equal("76900-000", atual.RootElement.GetProperty("cep").GetString());
        }

        [Fact]
        public async Task Atualizar_EmailVazio_MantemOEmailDaConta_Retorna204()
        {
            // O [EmailAddress] do .NET recusa a string vazia (só aceita nulo ou texto com um
            // único "@" fora das pontas); o DTO normaliza vazio/espaços para nulo antes da
            // validação, então vazio não é erro. Corretiva G2: para quem tem conta, vazio
            // significa "sem alteração" — a resposta continua trazendo o e-mail de acesso.
            Assert.False(new EmailAddressAttribute().IsValid(""));
            Assert.True(new EmailAddressAttribute().IsValid(null));
            Assert.True(new EmailAddressAttribute().IsValid("a@b"));
            Assert.False(new EmailAddressAttribute().IsValid("a@b@c"));

            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4d");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { Email = "   ", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            using var json = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.Equal(cenario.EmailProfessor, json.RootElement.GetProperty("email").GetString());
            Assert.True(json.RootElement.GetProperty("possuiConta").GetBoolean());
            Assert.Equal("Ji-Paraná", json.RootElement.GetProperty("cidade").GetString());
        }

        [Fact]
        public async Task Atualizar_EmailDiferenteDaConta_Retorna400ENaoGrava()
        {
            // Corretiva G2: o e-mail de quem tem conta só muda pela edição do usuário (RF14).
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4e");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new { Email = "professor.rf4e.novo@teste.com", Cidade = "Ji-Paraná" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
                Assert.Equal(EmailDaPessoa.MensagemDaConta, json.RootElement.GetProperty("mensagem").GetString());

            using var atual = JsonDocument.Parse(await (await professor.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync());
            Assert.Equal(cenario.EmailProfessor, atual.RootElement.GetProperty("email").GetString());
            Assert.NotEqual("Ji-Paraná", atual.RootElement.GetProperty("cidade").GetString());
        }

        [Fact]
        public async Task Atualizar_DadosValidos_Retorna204EPersiste()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf4c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            // O e-mail enviado é o da própria conta (o campo é somente leitura na tela).
            var resposta = await professor.PutAsJsonAsync("/api/meus-dados", new
            {
                Celular = "(69) 99999-1234",
                Email = cenario.EmailProfessor,
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
            Assert.Equal(cenario.EmailProfessor, raiz.GetProperty("email").GetString());
            Assert.Equal("Ji-Paraná", raiz.GetProperty("cidade").GetString());
            Assert.Equal("RO", raiz.GetProperty("estado").GetString());
            Assert.Equal("76900-000", raiz.GetProperty("cep").GetString());
        }
    }
}
