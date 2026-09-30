using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF33 — consolidação (RNFs 32.6, 33.1, 33.2, 33.3; CSU11 fluxo principal e FA1).
    public class RF33_ConsolidarAulaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF33_ConsolidarAulaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Consolidar_ComChamadaIncompleta_Retorna400ComAlunosSemRegistro()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33a");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto, (cenario.MatriculaAId, true));
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Contains("A chamada deve ser concluída antes da consolidação", json.RootElement.GetProperty("mensagem").GetString());

            var semRegistro = json.RootElement.GetProperty("alunosSemRegistro").EnumerateArray()
                .Select(a => (id: a.GetProperty("alunoDepartamentoId").GetInt32(), nome: a.GetProperty("nomeAluno").GetString()))
                .ToList();

            // Só o aluno B (matrícula ativa sem registro): A tem registro e o inativo não conta.
            var unico = Assert.Single(semRegistro);
            Assert.Equal((cenario.MatriculaBId, cenario.NomeAlunoB), unico);

            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        [Fact]
        public async Task Consolidar_ComChamadaCompleta_MudaSituacaoParaConsolidada()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33b");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);

            // A API passa a devolver a situação e a aula deixa de ser pendente de fechamento.
            var aula = await professor.GetAsync($"/api/aulas/{aulaId}");
            using var json = JsonDocument.Parse(await aula.Content.ReadAsStringAsync());
            Assert.Equal(SituacaoAula.Consolidada, json.RootElement.GetProperty("situacao").GetString());
            Assert.False(json.RootElement.GetProperty("pendenteFechamento").GetBoolean());
        }

        [Fact]
        public async Task Consolidar_AulaNaoRealizada_Retorna400()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33c");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.NaoRealizada);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser consolidadas.", await LerMensagemAsync(resposta));
            Assert.Equal(SituacaoAula.NaoRealizada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        [Fact]
        public async Task Consolidar_AulaJaConsolidada_Retorna400()
        {
            // Antes da etapa 3 a operação era idempotente (devolvia sucesso); a RNF 33.2
            // restringe a consolidação a aulas Em aberto.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33d");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser consolidadas.", await LerMensagemAsync(resposta));
        }

        [Fact]
        public async Task Consolidar_ProfessorSemAtribuicaoNaTurma_Retorna403()
        {
            // RNF 33.1: professor da mesma igreja, mas sem atribuição ativa nesta turma.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf33e");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var professorFora = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessorSemAtribuicao);

            var resposta = await professorFora.PatchAsync($"/api/aulas/{aulaId}/consolidar", content: null);

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
