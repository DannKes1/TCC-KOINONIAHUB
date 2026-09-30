using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF33 — reabertura (RNFs 33.5 e 33.6; CSU11 FA3: Consolidada ou Não realizada
    // voltam para Em aberto, preservando os registros de presença existentes).
    public class RF33_ReabrirAulaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF33_ReabrirAulaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Reabrir_ComoProfessor_Retorna403()
        {
            // RNF 33.5: mesmo com atribuição ativa na turma, o Professor não reabre.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33ra");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/reabrir", content: null);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        [Fact]
        public async Task Reabrir_ComoAdmin_VoltaParaEmAbertoEPreservaPresencas()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33rb");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsync($"/api/aulas/{aulaId}/reabrir", content: null);

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);

            // RNF 33.6: os dois registros continuam lá, com os mesmos valores.
            var presencas = await CenarioAula.LerPresencasAsync(_fabrica, aulaId);
            Assert.Equal(2, presencas.Count);
            Assert.True(presencas.Single(p => p.AlunoDepartamentoId == cenario.MatriculaAId).Presente);
            Assert.False(presencas.Single(p => p.AlunoDepartamentoId == cenario.MatriculaBId).Presente);

            // Reaberta, a aula volta a aceitar correção da chamada (32.2) e nova consolidação.
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var correcao = await professor.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaBId, Presente = true } }
            });
            var novaConsolidacao = await professor.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

            Assert.Equal(HttpStatusCode.OK, correcao.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, novaConsolidacao.StatusCode);
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
            Assert.True((await CenarioAula.LerPresencasAsync(_fabrica, aulaId)).Single(p => p.AlunoDepartamentoId == cenario.MatriculaBId).Presente);
        }

        [Fact]
        public async Task Reabrir_AulaNaoRealizada_VoltaParaEmAberto()
        {
            // CSU11 FA3: a reabertura vale também para aula Não realizada.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33rc");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.NaoRealizada);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsync($"/api/aulas/{aulaId}/reabrir", content: null);

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
            Assert.Empty(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
        }

        [Fact]
        public async Task Reabrir_AulaEmAberto_Retorna400()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33rd");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsync($"/api/aulas/{aulaId}/reabrir", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("A aula já está Em aberto.", json.RootElement.GetProperty("mensagem").GetString());
        }

        [Fact]
        public async Task Reabrir_AulaDeOutraIgreja_Retorna404()
        {
            // Isolamento por igreja: o Admin de outra igreja não enxerga a aula.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33re");
            var outra = await CenarioAula.CriarAsync(_fabrica, "rf33re2");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var adminDeOutraIgreja = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, outra.EmailAdmin);

            var resposta = await adminDeOutraIgreja.PatchAsync($"/api/aulas/{aulaId}/reabrir", content: null);

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }
    }
}
