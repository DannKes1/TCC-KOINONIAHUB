using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RNF 2.5 / 42.1 na API (ExigeAceiteTermoFiltro, decisão da 5.1): conta autenticada
    // sem aceite da versão vigente só alcança o termo e o logout; o resto devolve 403
    // com termoPendente. Depois do aceite, tudo volta a funcionar.
    public class RF2_GateTermoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF2_GateTermoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Theory]
        [InlineData("/api/meus-dados")]
        [InlineData("/api/pessoas")]
        [InlineData("/api/departamentos")]
        [InlineData("/api/usuarios")]
        public async Task RotaAutenticada_SemAceite_Retorna403ComTermoPendente(string rota)
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, $"gate-{rota.Replace('/', '-')}", aceitarTermo: false);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync(rota);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(ExigeAceiteTermoFiltro.Mensagem, json.RootElement.GetProperty("mensagem").GetString());
            Assert.True(json.RootElement.GetProperty("termoPendente").GetBoolean());
        }

        [Fact]
        public async Task Escrita_SemAceite_Retorna403AntesDeExecutar()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "gate-escrita", aceitarTermo: false);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync("/api/departamentos", new { Nome = "Turma Bloqueada", Tipo = "EBD" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

            // Depois do aceite a mesma escrita passa — e a turma não existia antes.
            await admin.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });
            var lista = await admin.GetAsync("/api/departamentos");
            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            Assert.DoesNotContain("Turma Bloqueada", await lista.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TermoELogout_SemAceite_Liberados()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "gate-livre", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var vigente = await professor.GetAsync("/api/termo/vigente");
            var aceitarErrado = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = "0.0" });
            var logout = await professor.PostAsync("/api/auth/logout", content: null);

            Assert.Equal(HttpStatusCode.OK, vigente.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, aceitarErrado.StatusCode); // chegou ao controller (não foi 403 do filtro)
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        }

        [Fact]
        public async Task AposAceite_RotasLiberadas()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "gate-depois", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            Assert.Equal(HttpStatusCode.Forbidden, (await professor.GetAsync("/api/meus-dados")).StatusCode);

            var aceitar = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });
            Assert.Equal(HttpStatusCode.OK, aceitar.StatusCode);

            // Mesma sessão (mesmo cookie): o aceite vale imediatamente, sem novo login.
            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync("/api/meus-dados")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync($"/api/aulas?departamentoId={cenario.TurmaComAtribuicaoId}")).StatusCode);
        }

        [Fact]
        public async Task ContaComAceite_NaoEhAfetada()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "gate-ok");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/pessoas")).StatusCode);
        }

        [Fact]
        public async Task RotasPublicas_NaoExigemAceite()
        {
            var cliente = _fabrica.CreateClient();

            var login = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = "ninguem@teste.com", Senha = "x" });
            var vigente = await cliente.GetAsync("/api/termo/vigente");

            Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode); // credenciais inválidas, não 403
            Assert.Equal(HttpStatusCode.OK, vigente.StatusCode);
        }

        [Fact]
        public async Task Login_ComCookieDeContaPendente_NaoEhBloqueado()
        {
            // Navegador que ainda carrega o cookie de uma conta pendente precisa conseguir
            // entrar de novo: [AllowAnonymous] dispensa o filtro.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "gate-relogin", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var login = await professor.PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailProfessor, Senha = CenarioAcessoPessoa.Senha });

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("termoPendente").GetBoolean());
        }
    }
}
