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
    // Corretiva G6. RF14 (RNF 14.5 proposta): a alteração do perfil de uma conta encerra as
    // sessões abertas dessa conta; o novo perfil vale a partir do próximo acesso. O perfil vai
    // dentro do token, e o ContaAtivaMiddleware passou a compará-lo com o perfil atual.
    // Cada sessão é um cliente com o próprio login: depois do 401 o cookie é apagado, então
    // uma segunda rota com o mesmo token antigo precisa de outra sessão aberta antes da troca.
    public class RF14_PerfilAlteradoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF14_PerfilAlteradoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task AdminRebaixado_TokenAntigo_Recebe401EmRotaDeGestaoEEmRotaComum()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14g6a");
            var (emailRebaixado, _) = await CenarioAcessoPessoa.CriarContaAsync(_fabrica, cenario.IgrejaId, "Admin", "rf14g6a2");
            var sessaoGestao = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailRebaixado);
            var sessaoComum = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailRebaixado);
            Assert.Equal(HttpStatusCode.OK, (await sessaoGestao.GetAsync("/api/usuarios")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await sessaoComum.GetAsync("/api/meus-dados")).StatusCode);

            await RebaixarAsync(cenario.EmailAdmin, emailRebaixado, "Professor");

            var gestao = await sessaoGestao.GetAsync("/api/usuarios");
            Assert.Equal(HttpStatusCode.Unauthorized, gestao.StatusCode);
            Assert.Equal(ContaAtivaMiddleware.MensagemPerfilAlterado, await LerMensagemAsync(gestao));
            Assert.Contains(gestao.Headers.GetValues("Set-Cookie"), c => c.StartsWith("kh_token=", StringComparison.Ordinal));

            var comum = await sessaoComum.GetAsync("/api/meus-dados");
            Assert.Equal(HttpStatusCode.Unauthorized, comum.StatusCode);
            Assert.Equal(ContaAtivaMiddleware.MensagemPerfilAlterado, await LerMensagemAsync(comum));

            // Entrando de novo, vale o perfil novo: Professor não lista usuários, mas usa o restante.
            var novaSessao = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailRebaixado);
            Assert.Equal(HttpStatusCode.Forbidden, (await novaSessao.GetAsync("/api/usuarios")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await novaSessao.GetAsync("/api/meus-dados")).StatusCode);
        }

        [Fact]
        public async Task AdminRebaixado_TokenAntigo_NaoAlteraPerfilNenhum()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14g6b");
            var (emailRebaixado, _) = await CenarioAcessoPessoa.CriarContaAsync(_fabrica, cenario.IgrejaId, "Admin", "rf14g6b2");
            var sessaoPropria = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailRebaixado);
            var sessaoOutra = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailRebaixado);
            var rebaixadoId = await UsuarioIdAsync(emailRebaixado);
            var professorId = await UsuarioIdAsync(cenario.EmailProfessor);

            await RebaixarAsync(cenario.EmailAdmin, emailRebaixado, "Professor");

            var voltarAAdmin = await sessaoPropria.PatchAsJsonAsync($"/api/usuarios/{rebaixadoId}", new { Perfil = "Admin" });
            var promoverOutro = await sessaoOutra.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Perfil = "Pastor" });

            Assert.Equal(HttpStatusCode.Unauthorized, voltarAAdmin.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, promoverOutro.StatusCode);
            Assert.Equal("Professor", await PerfilAsync(rebaixadoId));
            Assert.Equal("Professor", await PerfilAsync(professorId));
        }

        // A tela de edição sempre envia o perfil; salvar com o mesmo perfil (só e-mail ou
        // situação, por exemplo) não pode derrubar a sessão da conta.
        [Fact]
        public async Task EdicaoSemMudarOPerfil_SessaoContinuaValida()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14g6c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var professorId = await UsuarioIdAsync(cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Perfil = "Professor", Ativo = true });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync("/api/meus-dados")).StatusCode);
        }

        // ---------- apoio ----------

        private async Task RebaixarAsync(string emailAdmin, string emailAlvo, string perfil)
        {
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailAdmin);
            var alvoId = await UsuarioIdAsync(emailAlvo);
            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{alvoId}", new { Perfil = perfil });
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        }

        private async Task<int> UsuarioIdAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Usuarios.Where(u => u.Email == email).Select(u => u.Id).SingleAsync();
        }

        private async Task<string> PerfilAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Perfil).SingleAsync();
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }
}
