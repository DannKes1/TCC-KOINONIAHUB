using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.Servicos.Implementacoes;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Infraestrutura.Repositorios;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    public class RF14_UltimoAdminTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string MensagemUltimoAdmin =
            "Não é possível inativar ou alterar o perfil do único administrador ativo da igreja.";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF14_UltimoAdminTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task RebaixarPerfil_UnicoAdmin_Retorna400()
        {
            var cenario = await CenarioAdministradores.CriarAsync(_fabrica, "rf14a", segundoAdmin: false);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdminA);

            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminAId}", new { Perfil = "Professor" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MensagemUltimoAdmin, await LerMensagemAsync(resposta));
            Assert.Equal(("Admin", true), await LerPerfilEAtivoAsync(admin, cenario.AdminAId));
        }

        [Fact]
        public async Task Inativar_UnicoAdmin_Retorna400()
        {
            var cenario = await CenarioAdministradores.CriarAsync(_fabrica, "rf14b", segundoAdmin: false);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdminA);

            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminAId}", new { Ativo = false });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(("Admin", true), await LerPerfilEAtivoAsync(admin, cenario.AdminAId));
        }

        [Fact]
        public async Task Inativar_AdminComOutroAtivo_Sucesso()
        {
            var cenario = await CenarioAdministradores.CriarAsync(_fabrica, "rf14c", segundoAdmin: true);
            var adminA = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdminA);

            var resposta = await adminA.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminBId}", new { Ativo = false });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(("Admin", false), await LerPerfilEAtivoAsync(adminA, cenario.AdminBId!.Value));
        }

        [Fact]
        public async Task RebaixarPerfil_AdminComOutroAtivo_Sucesso()
        {
            var cenario = await CenarioAdministradores.CriarAsync(_fabrica, "rf14d", segundoAdmin: true);
            var adminA = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdminA);

            var resposta = await adminA.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminBId}", new { Perfil = "Pastor" });

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            Assert.Equal(("Pastor", true), await LerPerfilEAtivoAsync(adminA, cenario.AdminBId!.Value));
        }

        [Fact]
        public async Task Rebaixar_ProprioPerfil_AposInativarOutroAdmin_Retorna400()
        {
            var cenario = await CenarioAdministradores.CriarAsync(_fabrica, "rf14e", segundoAdmin: true);
            var adminA = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdminA);

            var inativaB = await adminA.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminBId}", new { Ativo = false });
            var rebaixaA = await adminA.PatchAsJsonAsync($"/api/usuarios/{cenario.AdminAId}", new { Perfil = "Superintendente" });

            Assert.Equal(HttpStatusCode.NoContent, inativaB.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, rebaixaA.StatusCode);
            Assert.Equal(MensagemUltimoAdmin, await LerMensagemAsync(rebaixaA));
            Assert.Equal(("Admin", true), await LerPerfilEAtivoAsync(adminA, cenario.AdminAId));
        }

        [Fact]
        public async Task Servico_InativarUnicoAdminPorOutroAtor_LancaExcecaoDaRnf14_3()
        {
            using var ctx = new ContextoDeTeste();

            var igreja = new Igreja { Nome = "Igreja RF14" };
            var admin = new Usuario { Email = "unico.admin@teste.com", SenhaHash = "hash", Perfil = "Admin", Igreja = igreja };
            var pastor = new Usuario { Email = "pastor@teste.com", SenhaHash = "hash", Perfil = "Pastor", Igreja = igreja };
            ctx.Db.AddRange(admin, pastor);
            await ctx.Db.SaveChangesAsync();

            var servico = new UsuarioServico(new UsuarioRepositorio(ctx.Db), ctx.Db);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                servico.AtualizarAsync(igreja.Id, admin.Id, usuarioLogadoId: pastor.Id, new UsuarioAtualizarRequisicaoDto { Ativo = false }));

            Assert.Equal(MensagemUltimoAdmin, ex.Message);

            using var leitura = ctx.NovoContexto();
            var relido = await leitura.Usuarios.SingleAsync(u => u.Id == admin.Id);
            Assert.True(relido.Ativo);
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }

        private static async Task<(string perfil, bool ativo)> LerPerfilEAtivoAsync(HttpClient admin, int usuarioId)
        {
            var resposta = await admin.GetAsync($"/api/usuarios/{usuarioId}");
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return (json.RootElement.GetProperty("perfil").GetString()!, json.RootElement.GetProperty("ativo").GetBoolean());
        }
    }

    public sealed class CenarioAdministradores
    {
        public int IgrejaId { get; init; }
        public int AdminAId { get; init; }
        public int? AdminBId { get; init; }
        public string EmailAdminA { get; init; } = string.Empty;

        public static async Task<CenarioAdministradores> CriarAsync(KoinoniaHubWebApplicationFactory fabrica, string sufixo, bool segundoAdmin)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(CenarioAcessoPessoa.Senha);
            var igreja = new Igreja { Nome = $"Igreja {sufixo}" };
            var emailAdminA = $"admin.a.{sufixo}@teste.com";

            var adminA = new Usuario { Email = emailAdminA, SenhaHash = senhaHash, Perfil = "Admin", Igreja = igreja };
            db.Add(adminA);

            Usuario? adminB = null;
            if (segundoAdmin)
            {
                adminB = new Usuario { Email = $"admin.b.{sufixo}@teste.com", SenhaHash = senhaHash, Perfil = "Admin", Igreja = igreja };
                db.Add(adminB);
            }

            await db.SaveChangesAsync();

            return new CenarioAdministradores
            {
                IgrejaId = igreja.Id,
                AdminAId = adminA.Id,
                AdminBId = adminB?.Id,
                EmailAdminA = emailAdminA
            };
        }
    }
}
