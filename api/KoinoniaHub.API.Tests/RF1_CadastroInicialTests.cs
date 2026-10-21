using System.Net;
using System.Net.Http.Json;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Cadastro inicial (RF1, CSU01), evidência de Segurança da 7.3: a senha do Administrador
    // criado com a igreja fica só como hash BCrypt (RNFs 1.2 e 2.2). O fluxo (login e
    // listagem) está em Etapa0_FumacaTests; o aceite do termo, em RF42_TermoTests.
    public class RF1_CadastroInicialTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF1_CadastroInicialTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task RegistrarAdmin_GravaSenhaSoComoHashBCrypt()
        {
            const string senhaAdmin = "Cadastro@123";
            const string email = "admin.rf1a@teste.com";

            var resposta = await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/registrar-admin", new
            {
                Igreja = new { Nome = "Igreja RF1 BCrypt" },
                EmailAdmin = email,
                SenhaAdmin = senhaAdmin,
                NomeAdmin = "Administrador RF1",
                AceiteTermoVersao = TermosDeUso.Vigente.Versao
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.DoesNotContain(senhaAdmin, await resposta.Content.ReadAsStringAsync());

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var usuario = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Email == email);

            // RNFs 1.2 / 2.2: só o hash BCrypt (custo >= 10, seção 4.8).
            AfirmacoesSenha.SoHashBCrypt(usuario.SenhaHash, senhaAdmin);
        }
    }
}
