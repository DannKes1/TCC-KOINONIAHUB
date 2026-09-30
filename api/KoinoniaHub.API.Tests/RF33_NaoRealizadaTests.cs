using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF33 — marcar como Não realizada (RNFs 33.1, 33.2, 33.4, 33.7; CSU11 FA2).
    public class RF33_NaoRealizadaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF33_NaoRealizadaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task MarcarNaoRealizada_ComPresencas_Retorna400()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33na");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto, (cenario.MatriculaAId, false));
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/nao-realizada", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Uma aula com registros de presença não pode ser marcada como Não realizada.", await LerMensagemAsync(resposta));
            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
            Assert.Single(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
        }

        [Fact]
        public async Task MarcarNaoRealizada_SemPresencas_MudaSituacao()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33nb");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/nao-realizada", content: null);

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(SituacaoAula.NaoRealizada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);

            // RNF 33.4: continua sem registros de presença e deixa de ser pendente de fechamento.
            Assert.Empty(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));

            var aula = await professor.GetAsync($"/api/aulas/{aulaId}");
            using var json = JsonDocument.Parse(await aula.Content.ReadAsStringAsync());
            Assert.Equal(SituacaoAula.NaoRealizada, json.RootElement.GetProperty("situacao").GetString());
            Assert.False(json.RootElement.GetProperty("pendenteFechamento").GetBoolean());
        }

        [Fact]
        public async Task MarcarNaoRealizada_AulaConsolidada_Retorna400()
        {
            // RNF 33.2: só aulas Em aberto.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33nc");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsync($"/api/aulas/{aulaId}/nao-realizada", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser marcadas como Não realizadas.", await LerMensagemAsync(resposta));
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        [Fact]
        public async Task MarcarNaoRealizada_ProfessorSemAtribuicaoNaTurma_Retorna403()
        {
            // RNF 33.1.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33nd");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professorFora = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessorSemAtribuicao);

            var resposta = await professorFora.PatchAsync($"/api/aulas/{aulaId}/nao-realizada", content: null);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }
}
