using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests
{
    // RF32 — lançamento da chamada: só em aula Em aberto (32.2) e um único registro
    // por (aula, matrícula) garantido pelo índice único (32.3).
    public class RF32_ChamadaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF32_ChamadaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Theory]
        [InlineData(SituacaoAula.Consolidada)]
        [InlineData(SituacaoAula.NaoRealizada)]
        public async Task Registrar_EmAulaForaDeEmAberto_Retorna400(string situacao)
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, $"rf32a-{situacao}");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, situacao);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaAId, Presente = true } }
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("Somente aulas Em aberto permitem lançar ou alterar a chamada.", json.RootElement.GetProperty("mensagem").GetString());
            Assert.Empty(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
        }

        [Fact]
        public async Task Registrar_EmAulaConsolidada_Retorna400()
        {
            // Nome do Plano 7.1; o caso Consolidada também está coberto pela Theory acima.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf32b");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.Consolidada, (cenario.MatriculaAId, true));
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaAId, Presente = false } }
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

            // O registro consolidado não foi alterado.
            var presenca = Assert.Single(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
            Assert.True(presenca.Presente);
        }

        [Fact]
        public async Task Registrar_MesmaMatriculaDuasVezes_AtualizaEmVezDeDuplicar()
        {
            // Pela API, o segundo lançamento para a mesma matrícula atualiza o registro
            // existente: continua havendo um único (aula, matrícula).
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf32c");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var primeiro = await professor.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaAId, Presente = true } }
            });
            var segundo = await professor.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaAId, Presente = false, Observacao = "Saiu mais cedo" } }
            });

            Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);

            var presenca = Assert.Single(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
            Assert.False(presenca.Presente);
            Assert.Equal("Saiu mais cedo", presenca.Observacao);
        }

        [Fact]
        public async Task Registrar_MesmaMatriculaDuasVezes_ViolaIndiceUnico()
        {
            // RNF 32.3 no banco: dois registros para o mesmo par (aula, matrícula) são
            // rejeitados pelo índice único, independentemente do serviço.
            using var ctx = new ContextoDeTeste();

            var igreja = new Igreja { Nome = "Igreja RF32" };
            var turma = new Departamento { Nome = "Turma RF32", Igreja = igreja };
            var materia = new Materia { Nome = "Materia RF32", Departamento = turma };
            var professor = new Pessoa { Nome = "Professor RF32", Igreja = igreja };
            var aluno = new Pessoa { Nome = "Aluno RF32", Igreja = igreja };
            var matricula = new AlunoDepartamento { Pessoa = aluno, Departamento = turma };
            var aula = new Aula { Data = DateTime.UtcNow.AddDays(-1), Materia = materia, Professor = professor };

            ctx.Db.AddRange(matricula, aula);
            ctx.Db.Add(new Presenca { Aula = aula, AlunoDepartamento = matricula, Presente = true });
            await ctx.Db.SaveChangesAsync();

            ctx.Db.Add(new Presenca { Aula = aula, AlunoDepartamento = matricula, Presente = false });

            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.Db.SaveChangesAsync());

            using var leitura = ctx.NovoContexto();
            Assert.Equal(1, await leitura.Presencas.CountAsync(p => p.AulaId == aula.Id && p.AlunoDepartamentoId == matricula.Id));
        }
    }
}
