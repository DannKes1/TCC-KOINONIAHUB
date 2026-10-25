using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // RF45 — editar e excluir aula Em aberto (RNFs 45.1 a 45.5; CSU09 FA1–FA3).
    public class RF45_EditarAulaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF45_EditarAulaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Editar_EmAberto_Retorna200EGetReflete()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45a");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var novaData = new DateTime(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc);

            var resposta = await professor.PutAsJsonAsync($"/api/aulas/{aulaId}", new
            {
                Data = novaData,
                Tema = "Tema editado",
                Conteudo = "Conteudo editado",
                MateriaId = cenario.MateriaId,
                ProfessorId = cenario.ProfessorPessoaId
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                Assert.Equal("Tema editado", json.RootElement.GetProperty("tema").GetString());
                Assert.Equal("Conteudo editado", json.RootElement.GetProperty("conteudo").GetString());
                Assert.False(json.RootElement.GetProperty("possuiPresencas").GetBoolean());
            }

            var get = await professor.GetAsync($"/api/aulas/{aulaId}");
            using var lido = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            Assert.Equal("Tema editado", lido.RootElement.GetProperty("tema").GetString());
            Assert.Equal("Conteudo editado", lido.RootElement.GetProperty("conteudo").GetString());
            Assert.Equal(novaData.Date, lido.RootElement.GetProperty("data").GetDateTime().Date);
            Assert.Equal(SituacaoAula.EmAberto, lido.RootElement.GetProperty("situacao").GetString());

            Assert.NotNull((await CenarioAula.LerAulaAsync(_fabrica, aulaId)).AtualizadoEm);
        }

        [Fact]
        public async Task Editar_AulaConsolidada_Retorna400()
        {
            // RNF 45.2.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45b");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, false));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/aulas/{aulaId}", CorpoValido(cenario, "Nao pode"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser editadas.", await LerMensagemAsync(resposta));
            Assert.Null((await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Tema);
        }

        [Fact]
        public async Task Editar_AulaNaoRealizada_Retorna400()
        {
            // RNF 45.2.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45c");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.NaoRealizada);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PutAsJsonAsync($"/api/aulas/{aulaId}", CorpoValido(cenario, "Nao pode"));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser editadas.", await LerMensagemAsync(resposta));
            Assert.Null((await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Tema);
        }

        [Fact]
        public async Task Editar_MateriaDeOutraTurma_Retorna400()
        {
            // RNF 45.4: a matéria tem de pertencer à turma da aula.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45d");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var materiaOutraTurmaId = await CriarMateriaEmOutraTurmaAsync(cenario.IgrejaId, "rf45d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/aulas/{aulaId}", new
            {
                Data = DateTime.UtcNow.Date,
                Tema = "Materia errada",
                MateriaId = materiaOutraTurmaId,
                ProfessorId = cenario.ProfessorPessoaId
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("A matéria informada não pertence à turma desta aula.", await LerMensagemAsync(resposta));
            Assert.Equal(cenario.MateriaId, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).MateriaId);
        }

        [Fact]
        public async Task Editar_ProfessorSemAtribuicaoDeProfessor_Retorna400()
        {
            // RNF 45.4 / 21.3: o responsável precisa de atribuição ativa de Professor na turma.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45e");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/aulas/{aulaId}", new
            {
                Data = DateTime.UtcNow.Date,
                MateriaId = cenario.MateriaId,
                ProfessorId = cenario.ProfessorSemAtribuicaoPessoaId
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Professor não encontrado ou sem atribuição ativa de Professor neste departamento.", await LerMensagemAsync(resposta));
            Assert.Equal(cenario.ProfessorPessoaId, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).ProfessorId);
        }

        [Fact]
        public async Task Editar_SemData_Retorna400EDataNaoMuda()
        {
            // RNF 45.3 (S6964): sem a data, o PUT volta 400 com o campo em errors, em vez
            // de gravar 0001-01-01 na aula.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45j");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var dataOriginal = await LerDataPelaApiAsync(professor, aulaId);

            var resposta = await professor.PutAsJsonAsync($"/api/aulas/{aulaId}", new
            {
                Tema = "Sem data",
                MateriaId = cenario.MateriaId,
                ProfessorId = cenario.ProfessorPessoaId
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
            {
                var erros = json.RootElement.GetProperty("errors").GetProperty("Data");
                Assert.Equal("Informe a data da aula.", Assert.Single(erros.EnumerateArray()).GetString());
            }

            Assert.Equal(dataOriginal, await LerDataPelaApiAsync(professor, aulaId));
            var lida = await CenarioAula.LerAulaAsync(_fabrica, aulaId);
            Assert.Null(lida.Tema);
            Assert.Null(lida.AtualizadoEm);
        }

        [Fact]
        public async Task Editar_ProfessorDeOutraTurma_Retorna403()
        {
            // RNF 45.1.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45f");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professorFora = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessorSemAtribuicao);

            var resposta = await professorFora.PutAsJsonAsync($"/api/aulas/{aulaId}", CorpoValido(cenario, "Invasao"));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Null((await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Tema);
        }

        [Fact]
        public async Task Excluir_SemPresencas_Retorna204ESomeDaListagem()
        {
            // RNF 45.5 / CSU09 FA2.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45g");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var outraAulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.DeleteAsync($"/api/aulas/{aulaId}");

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            // Desde a 7.1, aula inexistente é 404 para qualquer perfil (RecursoNaoEncontradoFiltro).
            Assert.Equal(HttpStatusCode.NotFound, (await professor.GetAsync($"/api/aulas/{aulaId}")).StatusCode);
            Assert.Null(await CenarioAula.LerAulaOuNuloAsync(_fabrica, aulaId));

            var lista = await professor.GetAsync($"/api/aulas?departamentoId={cenario.TurmaId}");
            using var json = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            var ids = json.RootElement.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();
            Assert.DoesNotContain(aulaId, ids);
            Assert.Contains(outraAulaId, ids);
        }

        [Fact]
        public async Task Excluir_ComPresencas_Retorna400()
        {
            // RNF 45.5: aula com registros permanece Em aberto até ser consolidada.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45h");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto, (cenario.MatriculaAId, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.DeleteAsync($"/api/aulas/{aulaId}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Uma aula com registros de presença não pode ser excluída.", await LerMensagemAsync(resposta));
            Assert.Equal(SituacaoAula.EmAberto, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
            Assert.Single(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));

            // A listagem avisa a tela para não oferecer Excluir (CSU09 FA3).
            var lista = await admin.GetAsync($"/api/aulas?departamentoId={cenario.TurmaId}");
            using var json = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            var aula = json.RootElement.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == aulaId);
            Assert.True(aula.GetProperty("possuiPresencas").GetBoolean());
        }

        [Fact]
        public async Task Excluir_AulaConsolidada_Retorna400()
        {
            // RNF 45.2.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf45i");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada,
                (cenario.MatriculaAId, true), (cenario.MatriculaBId, true));
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.DeleteAsync($"/api/aulas/{aulaId}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Somente aulas Em aberto podem ser excluídas.", await LerMensagemAsync(resposta));
            Assert.Equal(SituacaoAula.Consolidada, (await CenarioAula.LerAulaAsync(_fabrica, aulaId)).Situacao);
        }

        private static object CorpoValido(CenarioAula cenario, string tema) => new
        {
            Data = DateTime.UtcNow.Date,
            Tema = tema,
            MateriaId = cenario.MateriaId,
            ProfessorId = cenario.ProfessorPessoaId
        };

        // Segunda turma da mesma igreja, com uma matéria, para a RNF 45.4.
        private async Task<int> CriarMateriaEmOutraTurmaAsync(int igrejaId, string sufixo)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var igreja = await db.Igrejas.SingleAsync(i => i.Id == igrejaId);
            var outraTurma = new Departamento { Nome = $"Outra Turma {sufixo}", Igreja = igreja };
            var materia = new Materia { Nome = $"Materia Outra {sufixo}", Departamento = outraTurma };
            db.Add(materia);
            await db.SaveChangesAsync();

            return materia.Id;
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }

        private static async Task<string?> LerDataPelaApiAsync(HttpClient cliente, int aulaId)
        {
            var get = await cliente.GetAsync($"/api/aulas/{aulaId}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            using var json = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("data").GetString();
        }
    }
}
