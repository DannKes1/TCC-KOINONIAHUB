using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Corretiva G5. RF9 (RNF 9.3 proposta): inativar a pessoa inativa a conta de acesso e
    // descarta o convite pendente; reativar a pessoa não reativa a conta; não se inativa o
    // próprio cadastro nem a pessoa do último Admin ativo. RF14 (RNF 14.4 proposta): a conta
    // inativa perde o acesso na hora, mesmo com token válido.
    public class RF9_InativarPessoaContaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string MensagemLoginRecusado = "Usuário ou senha inválidos.";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF9_InativarPessoaContaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task InativarPessoaComConta_InativaAContaRecusaOLoginEDescartaOConvite()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf9a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var token = await GerarConviteAsync(admin, cenario.AlvoId);
            var pessoaId = await PessoaDaContaAsync(cenario.AlvoId);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(cenario.EmailAlvo)).StatusCode);

            var resposta = await admin.PutAsJsonAsync($"/api/pessoas/{pessoaId}", new { Nome = "Pessoa Alvo rf9a", Situacao = "Inativo" });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            var (ativo, conviteHash) = await LerContaAsync(cenario.AlvoId);
            Assert.False(ativo);
            Assert.Null(conviteHash);

            var login = await LoginAsync(cenario.EmailAlvo);
            Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
            Assert.Equal(MensagemLoginRecusado, await LerMensagemAsync(login));
            Assert.Equal(HttpStatusCode.NotFound, (await _fabrica.CreateClient().GetAsync($"/api/auth/primeiro-acesso/{token}")).StatusCode);
        }

        [Fact]
        public async Task ReativarPessoa_ContaContinuaInativa()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf9b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var pessoaId = await PessoaDaContaAsync(cenario.AlvoId);

            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/api/pessoas/{pessoaId}", new { Nome = "Pessoa Alvo rf9b", Situacao = "Inativo" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/api/pessoas/{pessoaId}", new { Nome = "Pessoa Alvo rf9b", Situacao = "Ativo" })).StatusCode);

            Assert.False((await LerContaAsync(cenario.AlvoId)).ativo);
            Assert.Equal(HttpStatusCode.BadRequest, (await LoginAsync(cenario.EmailAlvo)).StatusCode);
        }

        [Fact]
        public async Task InativarPessoaDoUnicoAdmin_Retorna400ENadaMuda()
        {
            // Quem inativa é um Superintendente (RNF 9.1): a guarda do próprio cadastro não se
            // aplica e a do último Admin é a que responde.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9c");
            var (emailSuper, _) = await CenarioAcessoPessoa.CriarContaAsync(_fabrica, cenario.IgrejaId, "Superintendente", "rf9c");
            var superintendente = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailSuper);
            var (adminId, adminPessoaId) = await ContaPorEmailAsync(cenario.EmailAdmin);

            var resposta = await superintendente.PutAsJsonAsync($"/api/pessoas/{adminPessoaId}", new { Nome = "Admin rf9c", Situacao = "Inativo" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Não é possível inativar a pessoa do único administrador ativo da igreja.", await LerMensagemAsync(resposta));
            Assert.True((await LerContaAsync(adminId)).ativo);
            Assert.Equal("Ativo", await LerSituacaoDaPessoaAsync(adminPessoaId));
        }

        [Fact]
        public async Task InativarOProprioCadastro_Retorna400ENadaMuda()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf9d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (adminId, adminPessoaId) = await ContaPorEmailAsync(cenario.EmailAdmin);

            var resposta = await admin.PutAsJsonAsync($"/api/pessoas/{adminPessoaId}", new { Nome = "Admin rf9d", Situacao = "Inativo" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("Você não pode inativar o seu próprio cadastro.", await LerMensagemAsync(resposta));
            Assert.True((await LerContaAsync(adminId)).ativo);
            Assert.Equal("Ativo", await LerSituacaoDaPessoaAsync(adminPessoaId));
        }

        // RF14 (RNF 14.4 proposta): sessão aberta de conta inativada recebe 401 numa rota
        // autenticada qualquer; rota pública continua respondendo.
        [Fact]
        public async Task ContaInativadaComTokenValido_Recebe401EmRotaAutenticada()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14g5a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync("/api/meus-dados")).StatusCode);

            var (professorId, _) = await ContaPorEmailAsync(cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Ativo = false })).StatusCode);

            var resposta = await professor.GetAsync("/api/meus-dados");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(ContaAtivaMiddleware.Mensagem, await LerMensagemAsync(resposta));
        }

        [Fact]
        public async Task ContaInativada_RotaPublicaContinuaRespondendo()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14g5b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var (professorId, _) = await ContaPorEmailAsync(cenario.EmailProfessor);

            using (var escopo = _fabrica.Services.CreateScope())
            {
                var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
                var conta = await db.Usuarios.SingleAsync(u => u.Id == professorId);
                conta.Ativo = false;
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync("/api/termo/vigente")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await professor.GetAsync("/api/meus-dados")).StatusCode);
        }

        // ---------- apoio ----------

        private async Task<HttpResponseMessage> LoginAsync(string email)
        {
            return await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = CenarioAcessoPessoa.Senha });
        }

        private static async Task<string> GerarConviteAsync(HttpClient admin, int usuarioId)
        {
            var resposta = await admin.PostAsync($"/api/usuarios/{usuarioId}/convite", content: null);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("token").GetString() ?? string.Empty;
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }

        private async Task<int> PessoaDaContaAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.PessoaId!.Value).SingleAsync();
        }

        private async Task<(int id, int pessoaId)> ContaPorEmailAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var conta = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Email == email);
            return (conta.Id, conta.PessoaId!.Value);
        }

        private async Task<(bool ativo, string? conviteHash)> LerContaAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var conta = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
            return (conta.Ativo, conta.ConviteTokenHash);
        }

        private async Task<string> LerSituacaoDaPessoaAsync(int pessoaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Pessoas.Where(p => p.Id == pessoaId).Select(p => p.Situacao).SingleAsync();
        }
    }
}
