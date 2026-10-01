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
    // RF27 / RNF 27.2: matrícula ativa duplicada por (turma, pessoa) é impedida — pelo
    // serviço (400) e, independentemente dele, pelo índice único filtrado do banco.
    public class RF27_MatriculaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF27_MatriculaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Matricular_JaAtiva_PelaApi_Retorna400()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf27a");
            var pessoaId = await LerPessoaIdAsync(cenario.MatriculaAId);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync($"/api/departamentos/{cenario.TurmaId}/matriculas", new { PessoaId = pessoaId });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("Esta pessoa já está matriculada (ativa) neste departamento.", json.RootElement.GetProperty("mensagem").GetString());
            Assert.Equal(1, await ContarMatriculasAtivasAsync(cenario.TurmaId, pessoaId));
        }

        [Fact]
        public async Task Matricular_JaAtiva_ViolaIndiceUnico()
        {
            // Direto no banco, sem passar pelo serviço: o índice único filtrado
            // (DepartamentoId, PessoaId) WHERE Ativo = true rejeita a segunda ativa.
            using var ctx = new ContextoDeTeste();

            var igreja = new Igreja { Nome = "Igreja RF27" };
            var turma = new Departamento { Nome = "Turma RF27", Igreja = igreja };
            var pessoa = new Pessoa { Nome = "Aluno RF27", Igreja = igreja };

            ctx.Db.Add(new AlunoDepartamento { Pessoa = pessoa, Departamento = turma });
            await ctx.Db.SaveChangesAsync();

            ctx.Db.Add(new AlunoDepartamento { Pessoa = pessoa, Departamento = turma });

            await Assert.ThrowsAsync<DbUpdateException>(() => ctx.Db.SaveChangesAsync());

            using var leitura = ctx.NovoContexto();
            Assert.Equal(1, await leitura.AlunosDepartamentos.CountAsync(m => m.DepartamentoId == turma.Id && m.PessoaId == pessoa.Id));
        }

        [Fact]
        public async Task Matricular_AposInativacao_PermiteNovaMatriculaAtiva()
        {
            // O índice é filtrado: a matrícula inativa (histórico) não bloqueia uma nova ativa.
            using var ctx = new ContextoDeTeste();

            var igreja = new Igreja { Nome = "Igreja RF27b" };
            var turma = new Departamento { Nome = "Turma RF27b", Igreja = igreja };
            var pessoa = new Pessoa { Nome = "Aluno RF27b", Igreja = igreja };

            ctx.Db.Add(new AlunoDepartamento { Pessoa = pessoa, Departamento = turma, Ativo = false, DataSaida = DateTime.UtcNow.AddMonths(-1) });
            await ctx.Db.SaveChangesAsync();

            ctx.Db.Add(new AlunoDepartamento { Pessoa = pessoa, Departamento = turma });
            await ctx.Db.SaveChangesAsync();

            using var leitura = ctx.NovoContexto();
            Assert.Equal(2, await leitura.AlunosDepartamentos.CountAsync(m => m.DepartamentoId == turma.Id && m.PessoaId == pessoa.Id));
            Assert.Equal(1, await leitura.AlunosDepartamentos.CountAsync(m => m.DepartamentoId == turma.Id && m.PessoaId == pessoa.Id && m.Ativo));
        }

        private async Task<int> LerPessoaIdAsync(int matriculaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.AlunosDepartamentos.AsNoTracking().SingleAsync(m => m.Id == matriculaId)).PessoaId;
        }

        private async Task<int> ContarMatriculasAtivasAsync(int departamentoId, int pessoaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.AlunosDepartamentos.CountAsync(m => m.DepartamentoId == departamentoId && m.PessoaId == pessoaId && m.Ativo);
        }
    }
}
