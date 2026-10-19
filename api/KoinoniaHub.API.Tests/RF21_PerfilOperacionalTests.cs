using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Etapa 7.1 — Autorização (Plano 6.16; monografia seção 4.9, RNFs 21.4 e 21.5).
    // Perfil = área do sistema que a conta alcança; atribuição ativa = turmas sobre as
    // quais ela age. Uma conta de perfil Usuario com atribuição ativa não opera a turma
    // (21.4); a listagem de atribuições informa `perfilConta` para a tela sinalizar
    // "Sem conta" e "Conta com perfil Usuário" (21.5); a FallbackPolicy protege rotas
    // sem atributo; POST /api/igrejas foi removido; turma, matéria e aula inexistentes
    // devolvem 404 para qualquer perfil (RecursoNaoEncontradoFiltro).
    public class RF21_PerfilOperacionalTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF21_PerfilOperacionalTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task ListarAulas_UsuarioComAtribuicaoAtiva_Retorna403()
        {
            // RNF 21.4: a atribuição existe e está ativa, mas o perfil Usuario não habilita a operação.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21a");
            var emailUsuario = await CriarUsuarioComAtribuicaoAsync(cenario, "rf21a");
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailUsuario);

            var resposta = await usuario.GetAsync($"/api/aulas?departamentoId={cenario.TurmaId}");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(AutorizacaoEbdServico.MensagemPerfilOperacional, await LerMensagemAsync(resposta));
        }

        [Fact]
        public async Task RegistrarPresenca_UsuarioComAtribuicaoAtiva_Retorna403ENadaGravado()
        {
            // RNF 21.4 na escrita: a chamada não é lançada por conta de perfil Usuario.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21b");
            var aulaId = await cenario.CriarAulaAsync(_fabrica, SituacaoAula.EmAberto);
            var emailUsuario = await CriarUsuarioComAtribuicaoAsync(cenario, "rf21b");
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailUsuario);

            var resposta = await usuario.PostAsJsonAsync($"/api/aulas/{aulaId}/presencas", new
            {
                Itens = new[] { new { AlunoDepartamentoId = cenario.MatriculaAId, Presente = true } }
            });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(AutorizacaoEbdServico.MensagemPerfilOperacional, await LerMensagemAsync(resposta));
            Assert.Empty(await CenarioAula.LerPresencasAsync(_fabrica, aulaId));
        }

        [Fact]
        public async Task ListarAulas_ProfessorComAtribuicaoAtiva_Retorna200()
        {
            // Perfil Professor + atribuição ativa: o caso que a 21.4 autoriza. (Professor sem
            // atribuição → 403 já está coberto em RF11 e RF34.)
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/aulas?departamentoId={cenario.TurmaId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task MinhasTurmas_UsuarioComAtribuicaoAtiva_SemVinculoDeAtribuicao()
        {
            // RNF 21.4 na lista de vínculos (RF5): o aluno A tem matrícula ativa e, como conta
            // Usuario, ganha atribuição de Auxiliar; a API devolve a turma só como "Aluno".
            // O Professor, com a mesma turma, vê o vínculo "Professor".
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21j");
            var emailUsuario = await CriarUsuarioComAtribuicaoAsync(cenario, "rf21j");
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailUsuario);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var doUsuario = await usuario.GetAsync("/api/meus-dados/minhas-turmas");
            var doProfessor = await professor.GetAsync("/api/meus-dados/minhas-turmas");

            Assert.Equal(HttpStatusCode.OK, doUsuario.StatusCode);
            using (var json = JsonDocument.Parse(await doUsuario.Content.ReadAsStringAsync()))
            {
                var item = Assert.Single(json.RootElement.EnumerateArray());
                Assert.Equal(cenario.TurmaId, item.GetProperty("departamentoId").GetInt32());
                Assert.Equal("Aluno", item.GetProperty("vinculo").GetString());
            }

            Assert.Equal(HttpStatusCode.OK, doProfessor.StatusCode);
            using (var json = JsonDocument.Parse(await doProfessor.Content.ReadAsStringAsync()))
            {
                var item = Assert.Single(json.RootElement.EnumerateArray());
                Assert.Equal("Professor", item.GetProperty("vinculo").GetString());
            }
        }

        [Fact]
        public async Task EndpointSemAtributo_SemSessaoRetorna401_ComSessaoRetorna200()
        {
            // FallbackPolicy RequireAuthenticatedUser() (Program.cs): rota sem [Authorize]
            // nem [AllowAnonymous] exige autenticação. Com sessão, responde normalmente —
            // prova de que o 401 veio da política, não de rota inexistente.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf21d");
            var anonimo = _fabrica.CreateClient();
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var semSessao = await anonimo.GetAsync("/api/testes/sem-atributo");
            var comSessao = await admin.GetAsync("/api/testes/sem-atributo");

            Assert.Equal(HttpStatusCode.Unauthorized, semSessao.StatusCode);
            Assert.Equal(HttpStatusCode.OK, comSessao.StatusCode);
        }

        [Fact]
        public async Task ListarAtribuicoes_PerfilConta_NuloSemContaEIgualAoPerfilComConta()
        {
            // RNF 21.5: Professor (conta Professor), aluno A (conta Usuario) e uma pessoa
            // sem conta, todos com atribuição ativa na turma.
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21e");
            await CriarUsuarioComAtribuicaoAsync(cenario, "rf21e");
            var pessoaAlunoAId = await PessoaIdDaMatriculaAsync(cenario.MatriculaAId);
            var pessoaSemContaId = await CriarPessoaSemContaComAtribuicaoAsync(cenario, "rf21e");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/atribuicoes/departamento/{cenario.TurmaId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var porPessoa = json.RootElement.EnumerateArray()
                .ToDictionary(a => a.GetProperty("pessoaId").GetInt32(), a => a.GetProperty("perfilConta"));

            Assert.Equal("Professor", porPessoa[cenario.ProfessorPessoaId].GetString());
            Assert.Equal("Usuario", porPessoa[pessoaAlunoAId].GetString());
            Assert.Equal(JsonValueKind.Null, porPessoa[pessoaSemContaId].ValueKind);
        }

        [Fact]
        public async Task CriarIgreja_RotaRemovida_Retorna404()
        {
            // Decisão do autor (7.1): a igreja nasce só pelo cadastro inicial (RF1). Como na
            // RNF 15.3, a rota removida devolve 404 e nada é gravado.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf21f");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var igrejasAntes = await ContarIgrejasAsync();

            var resposta = await admin.PostAsJsonAsync("/api/igrejas", new { Nome = "Igreja Paralela" });

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(igrejasAntes, await ContarIgrejasAsync());
        }

        [Fact]
        public async Task ObterAula_Inexistente_Retorna404ParaProfessorEGestao()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21g");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var doProfessor = await professor.GetAsync("/api/aulas/999999");
            var doAdmin = await admin.GetAsync("/api/aulas/999999");

            Assert.Equal(HttpStatusCode.NotFound, doProfessor.StatusCode);
            Assert.Equal("Aula não encontrada para esta igreja.", await LerMensagemAsync(doProfessor));
            Assert.Equal(HttpStatusCode.NotFound, doAdmin.StatusCode);
        }

        [Fact]
        public async Task ObterMateria_Inexistente_Retorna404ParaProfessorEGestao()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21h");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var doProfessor = await professor.GetAsync("/api/materias/999999");
            var doAdmin = await admin.GetAsync("/api/materias/999999");

            Assert.Equal(HttpStatusCode.NotFound, doProfessor.StatusCode);
            Assert.Equal("Matéria não encontrada para esta igreja.", await LerMensagemAsync(doProfessor));
            Assert.Equal(HttpStatusCode.NotFound, doAdmin.StatusCode);
        }

        [Fact]
        public async Task ListarAulas_TurmaInexistente_Retorna404ParaProfessorEGestao()
        {
            // Antes da 7.1 o Professor recebia 400 (guarda) e a gestão 400 (serviço).
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf21i");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var doProfessor = await professor.GetAsync("/api/aulas?departamentoId=999999");
            var doAdmin = await admin.GetAsync("/api/aulas?departamentoId=999999");

            Assert.Equal(HttpStatusCode.NotFound, doProfessor.StatusCode);
            Assert.Equal("Departamento não encontrado para esta igreja.", await LerMensagemAsync(doProfessor));
            Assert.Equal(HttpStatusCode.NotFound, doAdmin.StatusCode);
        }

        // ---------- apoio ----------

        // Conta de perfil Usuario para a pessoa do aluno A, com atribuição ativa de
        // Auxiliar na turma: o caso que a 21.4 recusa.
        private async Task<string> CriarUsuarioComAtribuicaoAsync(CenarioAula cenario, string sufixo)
        {
            var email = await CenarioAula.CriarUsuarioDaMatriculaAsync(_fabrica, cenario.MatriculaAId, sufixo);
            var pessoaId = await PessoaIdDaMatriculaAsync(cenario.MatriculaAId);

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            db.Add(new Atribuicao { Funcao = "Auxiliar", PessoaId = pessoaId, DepartamentoId = cenario.TurmaId });
            await db.SaveChangesAsync();

            return email;
        }

        private async Task<int> CriarPessoaSemContaComAtribuicaoAsync(CenarioAula cenario, string sufixo)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var pessoa = new Pessoa { Nome = $"Sem Conta {sufixo}", IgrejaId = cenario.IgrejaId };
            db.Add(new Atribuicao { Funcao = "Auxiliar", Pessoa = pessoa, DepartamentoId = cenario.TurmaId });
            await db.SaveChangesAsync();

            return pessoa.Id;
        }

        private async Task<int> PessoaIdDaMatriculaAsync(int matriculaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.AlunosDepartamentos.AsNoTracking()
                .Where(m => m.Id == matriculaId)
                .Select(m => m.PessoaId)
                .SingleAsync();
        }

        private async Task<int> ContarIgrejasAsync()
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Igrejas.CountAsync();
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }
}
