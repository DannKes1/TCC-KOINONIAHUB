using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Tempo;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Corretiva da Etapa 7.2 (descoberta pelo E2E-04): dia civil da igreja. Períodos
    // dos relatórios (RF35–RF38) e o "hoje" da pendência de fechamento (RNF 31.3/35.5)
    // são dias locais no fuso Igreja:FusoHorario (padrão America/Porto_Velho, UTC−4);
    // o banco continua em UTC. O relógio fica fixo em 18/10/2026 às 21h locais (já
    // 19/10 em UTC), o horário em que o critério antigo (dia UTC) errava.
    public class RF35_FusoHorarioTests : IClassFixture<FabricaComRelogioFixo>
    {
        private static readonly DateOnly Hoje = FabricaComRelogioFixo.Hoje;

        private readonly FabricaComRelogioFixo _fabrica;

        public RF35_FusoHorarioTests(FabricaComRelogioFixo fabrica)
        {
            _fabrica = fabrica;
        }

        // Instante UTC de `hora` locais (UTC−4) do dia `data`.
        private static DateTime Local(DateOnly data, int hora) =>
            data.ToDateTime(new TimeOnly(hora, 0), DateTimeKind.Utc).AddHours(4);

        private static RelogioIgreja NovoRelogio(string? fuso = null)
        {
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [RelogioIgreja.ChaveConfiguracao] = fuso })
                .Build();
            return new RelogioIgreja(configuracao, new RelogioFixo(FabricaComRelogioFixo.Agora));
        }

        [Fact]
        public void RelogioIgreja_DiaLocalEmInstantesUtc_EHojeAs21hLocais()
        {
            // Sem configuração vale o padrão (America/Porto_Velho, UTC−4, sem horário de verão).
            var relogio = NovoRelogio();

            Assert.Equal(TimeSpan.FromHours(-4), relogio.Fuso.BaseUtcOffset);
            Assert.Equal(Hoje, relogio.HojeLocal());
            Assert.Equal(new DateTime(2026, 10, 18, 4, 0, 0, DateTimeKind.Utc), relogio.InicioDoDiaUtc(Hoje));
            Assert.Equal(new DateTime(2026, 10, 19, 4, 0, 0, DateTimeKind.Utc).AddTicks(-1), relogio.FimDoDiaUtc(Hoje));
        }

        [Fact]
        public void RelogioIgreja_DataCivil_DataSemFusoEInstanteComFuso()
        {
            var relogio = NovoRelogio("SA Western Standard Time"); // id Windows também é aceito

            // "2026-10-18" da query string (sem fuso) e a meia-noite UTC do formato antigo: o próprio dia.
            Assert.Equal(Hoje, relogio.DataCivil(new DateTime(2026, 10, 18)));
            Assert.Equal(Hoje, relogio.DataCivil(new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)));
            // Um instante com fuso vira o dia local: 19/10 01h UTC ainda é 18/10.
            Assert.Equal(Hoje, relogio.DataCivil(FabricaComRelogioFixo.Agora.UtcDateTime));
        }

        [Fact]
        public async Task FrequenciaTurma_MatriculaAs23hLocaisDoDiaD_EntraComFimD()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "fuso1");

            // A foi matriculado às 23h locais de 18/10 (19/10 03h UTC); B, dez dias antes.
            await DefinirDataMatriculaAsync(cenario.MatriculaAId, Local(Hoje, 23));
            await DefinirDataMatriculaAsync(cenario.MatriculaBId, Local(Hoje.AddDays(-10), 9));

            // Aula de 18/10 como a tela grava (meia-noite local = 04h UTC), consolidada.
            await cenario.CriarAulaEmAsync(_fabrica, Local(Hoje, 0), SituacaoAula.Consolidada, 0,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var resposta = await admin.GetAsync(
                $"/api/relatorios/ebd/frequencia-turma?departamentoId={cenario.TurmaId}&dataInicio={Hoje.AddDays(-7):yyyy-MM-dd}&dataFim={Hoje:yyyy-MM-dd}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            // Com o dia UTC, o fim seria 18/10 23:59 UTC e A ficaria de fora (0 presenças).
            Assert.Equal(1, raiz.GetProperty("totalAulas").GetInt32());
            Assert.Equal(2, raiz.GetProperty("totalAlunos").GetInt32());
            Assert.Equal(1, raiz.GetProperty("totalPresentes").GetInt32());
            var alunoA = raiz.GetProperty("alunos").EnumerateArray().Single(a => a.GetProperty("matriculaId").GetInt32() == cenario.MatriculaAId);
            Assert.Equal(1, alunoA.GetProperty("presentes").GetInt32());
        }

        [Fact]
        public async Task AulaDeHoje_As21hLocais_NaoEhPendente()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "fuso2");
            var aulaHoje = await cenario.CriarAulaEmAsync(_fabrica, Local(Hoje, 0), SituacaoAula.EmAberto, 0);

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // Aula (RF31): Em aberto de hoje não é pendente de fechamento.
            Assert.False(await PendenteFechamentoAsync(admin, aulaHoje));

            // Relatórios (RF35, RF38): nem na lista de pendentes da turma, nem no resumo de hoje.
            var frequencia = await LerJsonAsync(admin,
                $"/api/relatorios/ebd/frequencia-turma?departamentoId={cenario.TurmaId}&dataInicio={Hoje.AddDays(-7):yyyy-MM-dd}&dataFim={Hoje:yyyy-MM-dd}");
            Assert.Equal(0, frequencia.GetProperty("aulasPendentes").GetArrayLength());

            var resumo = await LerJsonAsync(admin, $"/api/relatorios/ebd/resumo-dia?data={Hoje:yyyy-MM-dd}");
            var turma = resumo.GetProperty("turmas").EnumerateArray().Single(t => t.GetProperty("departamentoId").GetInt32() == cenario.TurmaId);
            Assert.Equal(1, turma.GetProperty("aulasEmAberto").GetInt32());
            Assert.False(turma.GetProperty("pendenteFechamento").GetBoolean());
            Assert.Equal(0, resumo.GetProperty("totalAulasPendentes").GetInt32());
        }

        [Fact]
        public async Task AulaDeOntem_EhPendente()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "fuso3");
            var aulaOntem = await cenario.CriarAulaEmAsync(_fabrica, Local(Hoje.AddDays(-1), 0), SituacaoAula.EmAberto, 0);

            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            Assert.True(await PendenteFechamentoAsync(admin, aulaOntem));

            var frequencia = await LerJsonAsync(admin,
                $"/api/relatorios/ebd/frequencia-turma?departamentoId={cenario.TurmaId}&dataInicio={Hoje.AddDays(-7):yyyy-MM-dd}&dataFim={Hoje:yyyy-MM-dd}");
            var pendente = Assert.Single(frequencia.GetProperty("aulasPendentes").EnumerateArray());
            Assert.Equal(aulaOntem, pendente.GetProperty("id").GetInt32());

            var resumo = await LerJsonAsync(admin, $"/api/relatorios/ebd/resumo-dia?data={Hoje.AddDays(-1):yyyy-MM-dd}");
            var turma = resumo.GetProperty("turmas").EnumerateArray().Single(t => t.GetProperty("departamentoId").GetInt32() == cenario.TurmaId);
            Assert.True(turma.GetProperty("pendenteFechamento").GetBoolean());
        }

        private async Task DefinirDataMatriculaAsync(int matriculaId, DateTime dataMatricula)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var matricula = await db.AlunosDepartamentos.SingleAsync(m => m.Id == matriculaId);
            matricula.DataMatricula = dataMatricula;
            await db.SaveChangesAsync();
        }

        private static async Task<bool> PendenteFechamentoAsync(HttpClient cliente, int aulaId)
        {
            var aula = await LerJsonAsync(cliente, $"/api/aulas/{aulaId}");
            return aula.GetProperty("pendenteFechamento").GetBoolean();
        }

        private static async Task<JsonElement> LerJsonAsync(HttpClient cliente, string url)
        {
            var resposta = await cliente.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.Clone();
        }
    }
}
