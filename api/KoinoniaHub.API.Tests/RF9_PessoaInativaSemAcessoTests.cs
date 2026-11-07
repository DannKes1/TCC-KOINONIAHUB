using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Regras;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Corretiva G6. RF9 (RNF 9.4 proposta): pessoa inativa não recebe acesso nem função. Com o
    // cadastro inativo, a API recusa com 400 e a mesma mensagem: criar conta, reativar conta,
    // criar atribuição, reativar atribuição e matricular. A pessoa é inativada pela própria
    // API (PUT /api/pessoas), que pela RNF 9.3 já inativa a conta e encerra as atribuições.
    public class RF9_PessoaInativaSemAcessoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF9_PessoaInativaSemAcessoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task CriarConta_PessoaInativa_Retorna400ENaoCria()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9g6a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            await InativarPessoaAsync(admin, cenario.AlunoForaId, "Aluno Fora rf9g6a");

            var resposta = await admin.PostAsJsonAsync("/api/usuarios", new
            {
                PessoaId = cenario.AlunoForaId,
                Email = "aluno.fora.rf9g6a@teste.com",
                Senha = CenarioAcessoPessoa.Senha,
                Perfil = "Usuario"
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(PessoaInativa.Mensagem, await LerMensagemAsync(resposta));
            Assert.False(await LerAsync(db => db.Usuarios.AnyAsync(u => u.PessoaId == cenario.AlunoForaId)));
        }

        [Fact]
        public async Task ReativarConta_PessoaInativa_Retorna400EContaContinuaInativa()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9g6b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var professorId = await UsuarioIdAsync(cenario.EmailProfessor);
            await InativarPessoaAsync(admin, cenario.ProfessorPessoaId, "Professor rf9g6b");

            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Perfil = "Professor", Ativo = true });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(PessoaInativa.Mensagem, await LerMensagemAsync(resposta));
            Assert.False(await ContaAtivaAsync(professorId));

            // Com o cadastro reativado em Pessoas, a conta pode ser reativada.
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/api/pessoas/{cenario.ProfessorPessoaId}", new { Nome = "Professor rf9g6b", Situacao = "Ativo" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Perfil = "Professor", Ativo = true })).StatusCode);
            Assert.True(await ContaAtivaAsync(professorId));
        }

        [Fact]
        public async Task CriarAtribuicao_PessoaInativa_Retorna400ENaoCria()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9g6c");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            await InativarPessoaAsync(admin, cenario.AlunoForaId, "Aluno Fora rf9g6c");

            var resposta = await admin.PostAsJsonAsync("/api/atribuicoes", new
            {
                PessoaId = cenario.AlunoForaId,
                DepartamentoId = cenario.TurmaComAtribuicaoId,
                Funcao = "Auxiliar",
                Ativo = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(PessoaInativa.Mensagem, await LerMensagemAsync(resposta));
            Assert.False(await LerAsync(db => db.Atribuicoes.AnyAsync(a => a.PessoaId == cenario.AlunoForaId)));
        }

        [Fact]
        public async Task ReativarAtribuicao_PessoaInativa_Retorna400EContinuaEncerrada()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9g6d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            await InativarPessoaAsync(admin, cenario.ProfessorPessoaId, "Professor rf9g6d");
            var atribuicaoId = await AtribuicaoDaPessoaAsync(cenario.ProfessorPessoaId);

            var resposta = await admin.PutAsJsonAsync($"/api/atribuicoes/{atribuicaoId}", new { Funcao = "Professor", Ativo = true });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(PessoaInativa.Mensagem, await LerMensagemAsync(resposta));
            Assert.False(await LerAsync(db => db.Atribuicoes.Where(a => a.Id == atribuicaoId).Select(a => a.Ativo).SingleAsync()));
        }

        [Fact]
        public async Task Matricular_PessoaInativa_Retorna400ENaoMatricula()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9g6e");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            await InativarPessoaAsync(admin, cenario.AlunoForaId, "Aluno Fora rf9g6e");

            var resposta = await admin.PostAsJsonAsync($"/api/departamentos/{cenario.TurmaComAtribuicaoId}/matriculas", new { PessoaId = cenario.AlunoForaId });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(PessoaInativa.Mensagem, await LerMensagemAsync(resposta));
            Assert.False(await LerAsync(db => db.AlunosDepartamentos.AnyAsync(m =>
                m.PessoaId == cenario.AlunoForaId && m.DepartamentoId == cenario.TurmaComAtribuicaoId)));
        }

        // ---------- apoio ----------

        private static async Task InativarPessoaAsync(HttpClient admin, int pessoaId, string nome)
        {
            var resposta = await admin.PutAsJsonAsync($"/api/pessoas/{pessoaId}", new { Nome = nome, Situacao = "Inativo" });
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        }

        private async Task<T> LerAsync<T>(Func<KoinoniaHubDbContext, Task<T>> consulta)
        {
            using var escopo = _fabrica.Services.CreateScope();
            return await consulta(escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>());
        }

        private Task<int> UsuarioIdAsync(string email) =>
            LerAsync(db => db.Usuarios.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());

        private Task<bool> ContaAtivaAsync(int usuarioId) =>
            LerAsync(db => db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Ativo).SingleAsync());

        private Task<int> AtribuicaoDaPessoaAsync(int pessoaId) =>
            LerAsync(db => db.Atribuicoes.Where(a => a.PessoaId == pessoaId).Select(a => a.Id).SingleAsync());

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }
}
