using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // Relatórios só com aulas Consolidadas (RNFs 35.5, 37.5, 38.4; RF36; CSU07, CSU15, CSU21;
    // Plano 6.9): Não realizadas ficam fora, Em aberto de data já ocorrida vai para
    // aulasPendentes[], e Em aberto futura ou de hoje não aparece em lugar nenhum.
    public class RF35_RelatorioTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF35_RelatorioTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        // Semente comum dos relatórios de turma: duas Consolidadas, uma Não realizada,
        // uma Em aberto já ocorrida (com chamada lançada, mas não consolidada) e uma futura.
        private sealed record AulasDaTurma(int Consolidada1, int Consolidada2, int NaoRealizada, int EmAbertoVencida, int EmAbertoFutura);

        private static async Task<AulasDaTurma> SemearAulasAsync(KoinoniaHubWebApplicationFactory fabrica, CenarioAula cenario)
        {
            var hoje = DateTime.UtcNow;
            var c1 = await cenario.CriarAulaEmAsync(fabrica, hoje.AddDays(-14), SituacaoAula.Consolidada, 2,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var c2 = await cenario.CriarAulaEmAsync(fabrica, hoje.AddDays(-7), SituacaoAula.Consolidada, 0,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var nr = await cenario.CriarAulaEmAsync(fabrica, hoje.AddDays(-5), SituacaoAula.NaoRealizada, 0);
            var vencida = await cenario.CriarAulaEmAsync(fabrica, hoje.AddDays(-3), SituacaoAula.EmAberto, 9,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var futura = await cenario.CriarAulaEmAsync(fabrica, hoje.AddDays(3), SituacaoAula.EmAberto, 0);
            return new AulasDaTurma(c1, c2, nr, vencida, futura);
        }

        private static string Periodo(int departamentoId) =>
            $"departamentoId={departamentoId}&dataInicio={DateTime.UtcNow.AddDays(-30):yyyy-MM-dd}&dataFim={DateTime.UtcNow.AddDays(10):yyyy-MM-dd}";

        [Fact]
        public async Task FrequenciaTurma_IgnoraAulasNaoRealizadasEEmAberto()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf35a");
            var aulas = await SemearAulasAsync(_fabrica, cenario);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/frequencia-turma?{Periodo(cenario.TurmaId)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // RNF 35.5: só as duas Consolidadas contam.
            Assert.Equal(2, raiz.GetProperty("totalAulas").GetInt32());
            var idsAulas = raiz.GetProperty("aulas").EnumerateArray().Select(a => a.GetProperty("aulaId").GetInt32()).ToList();
            Assert.Equal(new[] { aulas.Consolidada2, aulas.Consolidada1 }, idsAulas); // mais recente primeiro

            // A: 2 presenças; B: 2 ausências. A presença de ambos na aula Em aberto não entra.
            Assert.Equal(2, raiz.GetProperty("totalPresentes").GetInt32());
            Assert.Equal(2, raiz.GetProperty("totalAusentesMarcados").GetInt32());
            Assert.Equal(50m, raiz.GetProperty("percentualPresencaGeral").GetDecimal());

            var alunoB = raiz.GetProperty("alunos").EnumerateArray().Single(a => a.GetProperty("matriculaId").GetInt32() == cenario.MatriculaBId);
            Assert.Equal(0, alunoB.GetProperty("presentes").GetInt32());
            Assert.Equal(2, alunoB.GetProperty("ausentesMarcados").GetInt32());
            Assert.Equal(0m, alunoB.GetProperty("percentualPresenca").GetDecimal());
        }

        [Fact]
        public async Task FrequenciaTurma_ListaPendentesDeFechamento()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf35b");
            var aulas = await SemearAulasAsync(_fabrica, cenario);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/relatorios/ebd/frequencia-turma?{Periodo(cenario.TurmaId)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

            // Só a Em aberto de data já ocorrida: nem a Não realizada, nem a futura.
            var pendente = Assert.Single(json.RootElement.GetProperty("aulasPendentes").EnumerateArray());
            Assert.Equal(aulas.EmAbertoVencida, pendente.GetProperty("id").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(pendente.GetProperty("materia").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(pendente.GetProperty("professor").GetString()));
        }

        [Fact]
        public async Task Acompanhamento_SoConsolidadas_EListaPendentes()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf36a");
            var aulas = await SemearAulasAsync(_fabrica, cenario);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/acompanhamento?{Periodo(cenario.TurmaId)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            Assert.Equal(2, raiz.GetProperty("totalAulas").GetInt32());

            // B faltou às duas Consolidadas → 2 faltas consecutivas e 0%; a presença dele na
            // aula Em aberto, se contasse, zeraria a sequência e mudaria a classificação.
            var alunoB = Assert.Single(raiz.GetProperty("alunos").EnumerateArray());
            Assert.Equal(cenario.MatriculaBId, alunoB.GetProperty("matriculaId").GetInt32());
            Assert.Equal(2, alunoB.GetProperty("faltasConsecutivas").GetInt32());
            Assert.Equal(0m, alunoB.GetProperty("percentualPresenca").GetDecimal());
            Assert.Equal("Critico", alunoB.GetProperty("classificacao").GetString());

            Assert.Equal(aulas.EmAbertoVencida, Assert.Single(raiz.GetProperty("aulasPendentes").EnumerateArray()).GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task RankingFaltas_SoConsolidadas_EListaPendentes()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf37a");
            var aulas = await SemearAulasAsync(_fabrica, cenario);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/ranking-faltas?{Periodo(cenario.TurmaId)}&top=5");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // RNF 37.5: B lidera com 2 faltas em 2 aulas; A tem 0.
            var itens = raiz.GetProperty("itens").EnumerateArray().ToList();
            Assert.Equal(cenario.MatriculaBId, itens[0].GetProperty("matriculaId").GetInt32());
            Assert.Equal(2, itens[0].GetProperty("faltasTotais").GetInt32());
            Assert.Equal(2, itens[0].GetProperty("totalAulas").GetInt32());
            Assert.Equal(0, itens[1].GetProperty("faltasTotais").GetInt32());

            Assert.Equal(aulas.EmAbertoVencida, Assert.Single(raiz.GetProperty("aulasPendentes").EnumerateArray()).GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task MinhaFrequencia_SoConsolidadas_EListaPendentes()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf6a");
            var aulas = await SemearAulasAsync(_fabrica, cenario);
            var emailAluno = await CenarioAula.CriarUsuarioDaMatriculaAsync(_fabrica, cenario.MatriculaBId, "rf6a");
            var aluno = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailAluno);

            var resposta = await aluno.GetAsync($"/api/relatorios/ebd/minha-frequencia?{Periodo(cenario.TurmaId)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // CSU07: percentual e histórico só sobre as Consolidadas.
            Assert.Equal(2, raiz.GetProperty("totalAulas").GetInt32());
            Assert.Equal(0, raiz.GetProperty("presentes").GetInt32());
            Assert.Equal(2, raiz.GetProperty("ausentesMarcados").GetInt32());
            Assert.Equal(0m, raiz.GetProperty("percentualPresenca").GetDecimal());
            Assert.Equal(2, raiz.GetProperty("aulas").GetArrayLength());

            Assert.Equal(aulas.EmAbertoVencida, Assert.Single(raiz.GetProperty("aulasPendentes").EnumerateArray()).GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task ResumoDia_SomaSoConsolidadas()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf38a");
            var dia = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-7), DateTimeKind.Utc).AddHours(9);

            var consolidada = await cenario.CriarAulaEmAsync(_fabrica, dia, SituacaoAula.Consolidada, 3,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var naoRealizada = await cenario.CriarAulaEmAsync(_fabrica, dia.AddHours(1), SituacaoAula.NaoRealizada, 0);
            var emAbertoVencida = await cenario.CriarAulaEmAsync(_fabrica, dia.AddHours(2), SituacaoAula.EmAberto, 5,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var resposta = await admin.GetAsync($"/api/relatorios/ebd/resumo-dia?data={dia:yyyy-MM-dd}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // RNF 38.4: totais só da Consolidada (1 presente, 1 ausente, 3 visitantes).
            Assert.Equal(1, raiz.GetProperty("totalPresentes").GetInt32());
            Assert.Equal(1, raiz.GetProperty("totalAusentes").GetInt32());
            Assert.Equal(3, raiz.GetProperty("totalVisitantes").GetInt32());
            Assert.Equal(1, raiz.GetProperty("totalAulasConsolidadas").GetInt32());
            Assert.Equal(1, raiz.GetProperty("totalAulasNaoRealizadas").GetInt32());
            Assert.Equal(1, raiz.GetProperty("totalAulasPendentes").GetInt32());

            // As demais situações aparecem separadamente.
            Assert.Equal(naoRealizada, Assert.Single(raiz.GetProperty("aulasNaoRealizadas").EnumerateArray()).GetProperty("id").GetInt32());
            Assert.Equal(emAbertoVencida, Assert.Single(raiz.GetProperty("aulasPendentes").EnumerateArray()).GetProperty("id").GetInt32());

            var turma = raiz.GetProperty("turmas").EnumerateArray().Single(t => t.GetProperty("departamentoId").GetInt32() == cenario.TurmaId);
            Assert.True(turma.GetProperty("temChamada").GetBoolean());
            Assert.Equal(1, turma.GetProperty("presentes").GetInt32());
            Assert.Equal(3, turma.GetProperty("visitantes").GetInt32());
            Assert.Equal(1, turma.GetProperty("aulasConsolidadas").GetInt32());
            Assert.Equal(1, turma.GetProperty("aulasNaoRealizadas").GetInt32());
            Assert.Equal(1, turma.GetProperty("aulasEmAberto").GetInt32());
            Assert.True(turma.GetProperty("pendenteFechamento").GetBoolean());

            Assert.NotEqual(0, consolidada);
        }

        [Fact]
        public async Task ResumoDia_EmAbertoDeHoje_NaoEhPendente()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf38b");
            var hoje = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc).AddHours(9);
            await cenario.CriarAulaEmAsync(_fabrica, hoje, SituacaoAula.EmAberto, 0);

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var resposta = await admin.GetAsync($"/api/relatorios/ebd/resumo-dia?data={hoje:yyyy-MM-dd}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // Aula de hoje ainda pode ser fechada: é Em aberto, não pendente (RNF 38.4 / 31.3).
            Assert.Equal(0, raiz.GetProperty("aulasPendentes").GetArrayLength());
            var turma = raiz.GetProperty("turmas").EnumerateArray().Single(t => t.GetProperty("departamentoId").GetInt32() == cenario.TurmaId);
            Assert.Equal(1, turma.GetProperty("aulasEmAberto").GetInt32());
            Assert.False(turma.GetProperty("pendenteFechamento").GetBoolean());
        }

        [Fact]
        public async Task ResumoDia_ComoProfessor_Retorna403()
        {
            // RNF 38.1: resumo do dia é de gestão.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf38c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/relatorios/ebd/resumo-dia?data={DateTime.UtcNow:yyyy-MM-dd}");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }
    }
}
