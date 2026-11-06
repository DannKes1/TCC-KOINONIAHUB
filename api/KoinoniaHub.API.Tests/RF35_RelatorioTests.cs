using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Relatórios só com aulas Consolidadas (RNFs 35.5, 37.5, 38.4; RF36; CSU07, CSU15, CSU21;
    // registro do projeto, 6.9): Não realizadas ficam fora, Em aberto de data já ocorrida vai para
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

        // CSU15 (Entrega C da aderência): o percentual de cada aula é calculado sobre os
        // registros efetivos daquela aula. Uma matrícula criada depois da consolidação entra
        // no total de alunos e em "não registrado", mas não rebaixa o percentual da aula nem
        // move o gráfico de evolução (que lê o mesmo campo).
        [Fact]
        public async Task FrequenciaTurma_MatriculaCriadaDepoisDaAulaConsolidada_NaoAlteraPercentualDaAula()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf35h");
            await SemearAulasAsync(_fabrica, cenario);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var rota = $"/api/relatorios/ebd/frequencia-turma?{Periodo(cenario.TurmaId)}";

            var antes = await LerAulasDoRelatorioAsync(admin, rota);
            Assert.Equal(2, antes.Count);
            Assert.All(antes, a => Assert.Equal(50m, a.Percentual));

            var pessoaNovaId = await CriarPessoaAsync(cenario.IgrejaId, "Aluno Novo rf35h");
            var matricula = await admin.PostAsJsonAsync($"/api/departamentos/{cenario.TurmaId}/matriculas", new { PessoaId = pessoaNovaId });
            Assert.Equal(HttpStatusCode.OK, matricula.StatusCode);

            var resposta = await admin.GetAsync(rota);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            Assert.Equal(3, raiz.GetProperty("totalAlunos").GetInt32());
            Assert.Equal(50m, raiz.GetProperty("percentualPresencaGeral").GetDecimal());

            // A lista de aulas (fonte do gráfico de evolução) é a mesma: ids, ordem e percentuais.
            var depois = raiz.GetProperty("aulas").EnumerateArray()
                .Select(a => (Id: a.GetProperty("aulaId").GetInt32(), Percentual: a.GetProperty("percentualPresenca").GetDecimal()))
                .ToList();
            Assert.Equal(antes, depois);

            foreach (var aula in raiz.GetProperty("aulas").EnumerateArray())
            {
                Assert.Equal(3, aula.GetProperty("totalAlunos").GetInt32());
                Assert.Equal(1, aula.GetProperty("presentes").GetInt32());
                Assert.Equal(1, aula.GetProperty("ausentesMarcados").GetInt32());
                Assert.Equal(1, aula.GetProperty("naoRegistrado").GetInt32());
                Assert.Equal(50m, aula.GetProperty("percentualPresenca").GetDecimal());
            }

            var alunoNovo = raiz.GetProperty("alunos").EnumerateArray().Single(a => a.GetProperty("pessoaId").GetInt32() == pessoaNovaId);
            Assert.Equal(0, alunoNovo.GetProperty("presentes").GetInt32());
            Assert.Equal(0, alunoNovo.GetProperty("ausentesMarcados").GetInt32());
            Assert.Equal(2, alunoNovo.GetProperty("naoRegistrado").GetInt32());
            Assert.Equal(0m, alunoNovo.GetProperty("percentualPresenca").GetDecimal());
        }

        [Fact]
        public async Task FrequenciaTurma_AulaConsolidadaSemRegistro_PercentualZero()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf35i");
            var semRegistro = await cenario.CriarAulaEmAsync(_fabrica, DateTime.UtcNow.AddDays(-2), SituacaoAula.Consolidada, 0);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/frequencia-turma?{Periodo(cenario.TurmaId)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var aula = json.RootElement.GetProperty("aulas").EnumerateArray().Single(a => a.GetProperty("aulaId").GetInt32() == semRegistro);

            Assert.Equal(0, aula.GetProperty("presentes").GetInt32());
            Assert.Equal(0, aula.GetProperty("ausentesMarcados").GetInt32());
            Assert.Equal(2, aula.GetProperty("naoRegistrado").GetInt32());
            Assert.Equal(0m, aula.GetProperty("percentualPresenca").GetDecimal());
            Assert.Equal(0m, json.RootElement.GetProperty("percentualPresencaGeral").GetDecimal());
        }

        private static async Task<List<(int Id, decimal Percentual)>> LerAulasDoRelatorioAsync(HttpClient cliente, string rota)
        {
            var resposta = await cliente.GetAsync(rota);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("aulas").EnumerateArray()
                .Select(a => (Id: a.GetProperty("aulaId").GetInt32(), Percentual: a.GetProperty("percentualPresenca").GetDecimal()))
                .ToList();
        }

        private async Task<int> CriarPessoaAsync(int igrejaId, string nome)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var pessoa = new Pessoa { Nome = nome, IgrejaId = igrejaId };
            db.Add(pessoa);
            await db.SaveChangesAsync();
            return pessoa.Id;
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

            // RNF 37.5: B tem 2 faltas em 2 aulas Consolidadas; A, sem falta, fica fora (RF37).
            var item = Assert.Single(raiz.GetProperty("itens").EnumerateArray());
            Assert.Equal(cenario.MatriculaBId, item.GetProperty("matriculaId").GetInt32());
            Assert.Equal(2, item.GetProperty("faltasTotais").GetInt32());
            Assert.Equal(2, item.GetProperty("totalAulas").GetInt32());

            Assert.Equal(aulas.EmAbertoVencida, Assert.Single(raiz.GetProperty("aulasPendentes").EnumerateArray()).GetProperty("id").GetInt32());
        }

        // Corretiva G5 (RF37): o ranking lista só quem faltou, mesmo com posições sobrando.
        [Fact]
        public async Task RankingFaltas_SoAlunosComFalta_MesmoComPosicoesSobrando()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf37b");
            var matriculaC = await MatricularAsync(_fabrica, cenario, "Aluno C rf37b");
            var hoje = DateTime.UtcNow;
            await cenario.CriarAulaEmAsync(_fabrica, hoje.AddDays(-14), SituacaoAula.Consolidada, 0,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false), (matriculaC, false));
            await cenario.CriarAulaEmAsync(_fabrica, hoje.AddDays(-7), SituacaoAula.Consolidada, 0,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false), (matriculaC, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/ranking-faltas?{Periodo(cenario.TurmaId)}&top=10");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var itens = json.RootElement.GetProperty("itens").EnumerateArray()
                .Select(i => (Matricula: i.GetProperty("matriculaId").GetInt32(), Faltas: i.GetProperty("faltasTotais").GetInt32()))
                .ToList();
            Assert.Equal(new[] { (cenario.MatriculaBId, 2), (matriculaC, 1) }, itens);
        }

        // Corretiva G5 (RF37): ninguém faltou no período, a lista vem vazia.
        [Fact]
        public async Task RankingFaltas_NinguemFaltou_ListaVazia()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf37c");
            await cenario.CriarAulaEmAsync(_fabrica, DateTime.UtcNow.AddDays(-7), SituacaoAula.Consolidada, 0,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/relatorios/ebd/ranking-faltas?{Periodo(cenario.TurmaId)}&top=10");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(0, json.RootElement.GetProperty("itens").GetArrayLength());
        }

        private static async Task<int> MatricularAsync(KoinoniaHubWebApplicationFactory fabrica, CenarioAula cenario, string nome)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var matricula = new AlunoDepartamento
            {
                Pessoa = new Pessoa { Nome = nome, IgrejaId = cenario.IgrejaId },
                DepartamentoId = cenario.TurmaId
            };
            db.Add(matricula);
            await db.SaveChangesAsync();
            return matricula.Id;
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

        // Validação de entrada dos relatórios de período (RF6, RF35–RF37): dataFim anterior a
        // dataInicio → 400 com a mensagem única, antes de consultar a turma ou o serviço.
        [Theory]
        [InlineData("frequencia-turma")]
        [InlineData("acompanhamento")]
        [InlineData("ranking-faltas")]
        [InlineData("minha-frequencia")]
        public async Task Relatorios_PeriodoInvertido_Retorna400(string relatorio)
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, $"rf35p-{relatorio}");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync(
                $"/api/relatorios/ebd/{relatorio}?departamentoId={cenario.TurmaId}&dataInicio=2026-10-20&dataFim=2026-10-10");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("dataFim não pode ser menor que dataInicio.", json.RootElement.GetProperty("mensagem").GetString());
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
