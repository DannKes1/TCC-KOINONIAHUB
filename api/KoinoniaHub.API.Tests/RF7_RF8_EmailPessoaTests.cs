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
    // Corretiva G2 (RF7/RF8, RNF de unicidade do e-mail; RNF 13.5): um e-mail por pessoa na
    // igreja, entre cadastros e contas de acesso. Pessoa sem conta não pode receber o e-mail
    // de outra pessoa nem o da conta de outra; pessoa com conta tem o e-mail da conta e só o
    // Administrador o altera, pela edição do usuário (RF14).
    public class RF7_RF8_EmailPessoaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF7_RF8_EmailPessoaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Criar_EmailDeOutraPessoaOuDaContaDeOutra_Retorna400ENaoCria()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf7a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // Conta de outra pessoa (a do Professor, cujo cadastro nem tem e-mail) e cadastro de
            // outra pessoa (Aluno Dentro), em maiúsculas: a comparação ignora caixa.
            foreach (var email in new[] { cenario.EmailProfessor, "ALUNO.DENTRO.rf7a@teste.com" })
            {
                var resposta = await admin.PostAsJsonAsync("/api/pessoas", new { Nome = "Pessoa Nova rf7a", Email = email, Situacao = "Ativo" });

                Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
                Assert.Equal(EmailDaPessoa.MensagemEmUso, await LerMensagemAsync(resposta));
            }

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.False(await db.Pessoas.AnyAsync(p => p.Nome == "Pessoa Nova rf7a"));
        }

        [Fact]
        public async Task Atualizar_PessoaSemConta_EmailDaContaDeOutra_Retorna400()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf8a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var recusada = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.AlunoForaId}",
                new { Nome = "Aluno Fora rf8a", Email = cenario.EmailProfessor, Situacao = "Ativo" });

            Assert.Equal(HttpStatusCode.BadRequest, recusada.StatusCode);
            Assert.Equal(EmailDaPessoa.MensagemEmUso, await LerMensagemAsync(recusada));
            Assert.Null(await LerEmailNoBancoAsync(cenario.AlunoForaId));

            // Um e-mail livre entra normalmente: pessoa sem conta edita o próprio e-mail.
            var aceita = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.AlunoForaId}",
                new { Nome = "Aluno Fora rf8a", Email = "Aluno.Fora.rf8a@teste.com", Situacao = "Ativo" });

            Assert.Equal(HttpStatusCode.NoContent, aceita.StatusCode);
            Assert.Equal("aluno.fora.rf8a@teste.com", await LerEmailNoBancoAsync(cenario.AlunoForaId));
        }

        [Fact]
        public async Task Atualizar_PessoaComConta_EmailDiferenteDaConta_Retorna400()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf8b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.ProfessorPessoaId}",
                new { Nome = "Professor rf8b", Email = "professor.outro.rf8b@teste.com", Situacao = "Ativo" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(EmailDaPessoa.MensagemDaConta, await LerMensagemAsync(resposta));

            // Nem o cadastro nem o login mudaram.
            Assert.Null(await LerEmailNoBancoAsync(cenario.ProfessorPessoaId));
            Assert.Equal(HttpStatusCode.OK, (await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
                new { Email = cenario.EmailProfessor, Senha = CenarioAcessoPessoa.Senha })).StatusCode);
        }

        [Fact]
        public async Task PessoaComConta_ExibeOEmailDaConta_EGravarComEleOuVazio_Retorna204EIguala()
        {
            // O cadastro do Professor da semente não tem e-mail; a conta tem. A leitura mostra
            // o da conta com possuiConta, e a primeira gravação (vazio ou o mesmo e-mail, em
            // qualquer caixa) iguala o cadastro à conta.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf8c");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var (email, possuiConta) = await LerPessoaAsync(admin, cenario.ProfessorPessoaId);
            Assert.Equal(cenario.EmailProfessor, email);
            Assert.True(possuiConta);
            Assert.Null(await LerEmailNoBancoAsync(cenario.ProfessorPessoaId));

            var vazio = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.ProfessorPessoaId}",
                new { Nome = "Professor rf8c", Email = (string?)null, Situacao = "Ativo" });
            Assert.Equal(HttpStatusCode.NoContent, vazio.StatusCode);
            Assert.Equal(cenario.EmailProfessor, await LerEmailNoBancoAsync(cenario.ProfessorPessoaId));

            var mesmo = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.ProfessorPessoaId}",
                new { Nome = "Professor rf8c", Email = cenario.EmailProfessor.ToUpperInvariant(), Situacao = "Ativo" });
            Assert.Equal(HttpStatusCode.NoContent, mesmo.StatusCode);
            Assert.Equal(cenario.EmailProfessor, await LerEmailNoBancoAsync(cenario.ProfessorPessoaId));

            // Na lista, quem não tem conta vem com possuiConta = false.
            using var lista = JsonDocument.Parse(await (await admin.GetAsync("/api/pessoas")).Content.ReadAsStringAsync());
            var alunoFora = lista.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == cenario.AlunoForaId);
            Assert.False(alunoFora.GetProperty("possuiConta").GetBoolean());
        }

        private static async Task<(string? email, bool possuiConta)> LerPessoaAsync(HttpClient cliente, int pessoaId)
        {
            var resposta = await cliente.GetAsync($"/api/pessoas/{pessoaId}");
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return (json.RootElement.GetProperty("email").GetString(), json.RootElement.GetProperty("possuiConta").GetBoolean());
        }

        private async Task<string?> LerEmailNoBancoAsync(int pessoaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.Pessoas.AsNoTracking().SingleAsync(p => p.Id == pessoaId)).Email;
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }
}
