using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    public class RF34_PresencasPessoaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF34_PresencasPessoaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Presencas_ProfessorForaDaTurma_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf34a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoForaId}/presencas");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Presencas_ProfessorNaTurma_FiltraPelasSuasTurmas()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf34b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var doProfessor = await professor.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}/presencas");
            var doAdmin = await admin.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}/presencas");

            Assert.Equal(HttpStatusCode.OK, doProfessor.StatusCode);
            Assert.Equal(HttpStatusCode.OK, doAdmin.StatusCode);

            using var jsonProfessor = JsonDocument.Parse(await doProfessor.Content.ReadAsStringAsync());
            using var jsonAdmin = JsonDocument.Parse(await doAdmin.Content.ReadAsStringAsync());

            var itensProfessor = jsonProfessor.RootElement.EnumerateArray().ToList();
            var itensAdmin = jsonAdmin.RootElement.EnumerateArray().ToList();

            Assert.Equal(2, itensAdmin.Count);
            Assert.Single(itensProfessor);
            Assert.Equal(cenario.TurmaComAtribuicaoId, itensProfessor[0].GetProperty("departamentoId").GetInt32());

            // Etapa 4.2: cada registro informa a situação da aula (as aulas da semente estão Em aberto),
            // para o painel do aluno contar frequência só sobre Consolidadas (RF3 / CSU07).
            Assert.All(itensAdmin, item => Assert.Equal("EmAberto", item.GetProperty("situacaoAula").GetString()));
        }

        [Fact]
        public async Task Presencas_UsuarioComum_SoAsProprias()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf34c");
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailUsuarioComum);

            var outra = await usuario.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}/presencas");
            var propria = await usuario.GetAsync($"/api/pessoas/{cenario.UsuarioComumPessoaId}/presencas");

            Assert.Equal(HttpStatusCode.Forbidden, outra.StatusCode);
            Assert.Equal(HttpStatusCode.OK, propria.StatusCode);
        }

        // CSU12 FA1 / RF29: a inativação preserva o histórico, e o Professor da turma
        // continua a consultá-lo (RNF 34.3 restringe por turma, não pela situação da
        // matrícula). Professor de outra turma segue recebendo 403.
        [Fact]
        public async Task Presencas_ProfessorNaTurma_MatriculaInativa_Retorna200ComAsPresencasDaTurma()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf34d");
            await InativarMatriculaAsync(cenario.AlunoDentroId, cenario.TurmaComAtribuicaoId);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}/presencas");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var itens = json.RootElement.EnumerateArray().ToList();
            Assert.Single(itens);
            Assert.Equal(cenario.TurmaComAtribuicaoId, itens[0].GetProperty("departamentoId").GetInt32());
        }

        [Fact]
        public async Task Presencas_ProfessorForaDaTurma_MatriculaInativa_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf34e");
            await InativarMatriculaAsync(cenario.AlunoForaId, cenario.TurmaSemAtribuicaoId);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoForaId}/presencas");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        private async Task InativarMatriculaAsync(int pessoaId, int departamentoId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var matricula = await db.AlunosDepartamentos
                .SingleAsync(m => m.PessoaId == pessoaId && m.DepartamentoId == departamentoId);
            matricula.Ativo = false;
            matricula.DataSaida = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }
}
