using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests
{

    public class Etapa1_SituacaoAulaTests
    {
        [Fact]
        public async Task Aula_NovaSemInformarSituacao_PersisteComoEmAberto()
        {
            using var ctx = new ContextoDeTeste();

            var aula = NovaAulaMinima();
            ctx.Db.Aulas.Add(aula);
            await ctx.Db.SaveChangesAsync();

            using var leitura = ctx.NovoContexto();
            var lida = await leitura.Aulas.SingleAsync(a => a.Id == aula.Id);

            Assert.Equal(SituacaoAula.EmAberto, lida.Situacao);
        }

        [Theory]
        [InlineData(SituacaoAula.Consolidada)]
        [InlineData(SituacaoAula.NaoRealizada)]
        public async Task Aula_ComSituacaoAlterada_PersisteOValorGravado(string situacao)
        {
            using var ctx = new ContextoDeTeste();

            var aula = NovaAulaMinima();
            ctx.Db.Aulas.Add(aula);
            await ctx.Db.SaveChangesAsync();

            aula.Situacao = situacao;
            await ctx.Db.SaveChangesAsync();

            using var leitura = ctx.NovoContexto();
            var lida = await leitura.Aulas.SingleAsync(a => a.Id == aula.Id);

            Assert.Equal(situacao, lida.Situacao);
        }

        [Fact]
        public void SituacaoAula_ExpoeExatamenteOsTresValoresDaMonografia()
        {
            Assert.Equal(
                new[] { "EmAberto", "Consolidada", "NaoRealizada" },
                SituacaoAula.Todas);
        }

        private static Aula NovaAulaMinima()
        {
            var igreja = new Igreja { Nome = "Igreja de Teste" };
            var departamento = new Departamento { Nome = "Turma de Teste", Igreja = igreja };
            var materia = new Materia { Nome = "Matéria de Teste", Departamento = departamento };
            var professor = new Pessoa { Nome = "Professor de Teste", Igreja = igreja };

            return new Aula
            {
                Data = DateTime.UtcNow,
                Materia = materia,
                Professor = professor
            };
        }
    }
}
