using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Gestão de usuários (RF13, RF14, RF16; CSU19), evidências de Segurança da 7.3:
    // conta criada com senha informada pelo Admin guarda só o hash BCrypt (RNF 13.2) e as
    // rotas de /api/usuarios são só do Administrador (RNFs 13.1, 14.1, 16.1) — nem Professor,
    // nem Pastor ou Superintendente. O convite
    // (15.1/39.1) já é coberto por RF39_RF40_ConviteTests.
    public class RF13_UsuarioTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF13_UsuarioTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Criar_ComSenhaInformada_GravaSoHashBCrypt()
        {
            const string senhaInicial = "Inicial@123";
            const string email = "nova.conta.rf13a@teste.com";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf13a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync("/api/usuarios", new
            {
                PessoaId = cenario.AlunoForaId,
                Email = email,
                Senha = senhaInicial,
                Perfil = "Usuario"
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            var corpo = await resposta.Content.ReadAsStringAsync();
            Assert.DoesNotContain(senhaInicial, corpo);
            using (var json = JsonDocument.Parse(corpo))
                Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("conviteToken").ValueKind); // senha informada: sem convite

            // RNF 13.2: no banco, só o hash BCrypt (custo >= 10, seção 4.8).
            AfirmacoesSenha.SoHashBCrypt(await LerSenhaHashAsync(email), senhaInicial);

            // E o hash confere a senha: a conta nova entra (com o termo pendente, RNF 13.4).
            var login = await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = senhaInicial });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }

        [Theory]
        [InlineData("GET")]   // RNF 16.1: listar usuários
        [InlineData("POST")]  // RNF 13.1: cadastrar usuário
        [InlineData("PATCH")] // RNF 14.1: editar usuário (aqui, tentar se promover a Admin)
        public async Task RotasDeUsuarios_ComoProfessor_Retorna403(string metodo)
        {
            var sufixo = $"rf13b{metodo.ToLowerInvariant()}";
            var emailNovo = $"tentativa.{sufixo}@teste.com";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, sufixo);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var professorUsuarioId = await LerUsuarioIdAsync(cenario.EmailProfessor);

            var resposta = metodo switch
            {
                "GET" => await professor.GetAsync("/api/usuarios"),
                "POST" => await professor.PostAsJsonAsync("/api/usuarios", new
                {
                    PessoaId = cenario.AlunoForaId,
                    Email = emailNovo,
                    Senha = "Tentativa@123",
                    Perfil = "Admin"
                }),
                _ => await professor.PatchAsJsonAsync($"/api/usuarios/{professorUsuarioId}", new { Perfil = "Admin" })
            };

            // RNFs 13.1/14.1/16.1: a operação é restrita ao perfil Administrador…
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

            // …e nada mudou: nenhuma conta nova, e o Professor continua Professor.
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.False(await db.Usuarios.AnyAsync(u => u.Email == emailNovo));
            Assert.Equal("Professor", (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == professorUsuarioId)).Perfil);
        }

        [Theory]
        [InlineData("Pastor")]
        [InlineData("Superintendente")]
        public async Task ListarUsuarios_ComoPastorOuSuperintendente_Retorna403(string perfil)
        {
            // "Restrita ao perfil Administrador": os outros perfis administrativos também ficam
            // de fora (é o caso que distingue Roles = "Admin" de um atributo de gestão).
            var sufixo = $"rf13c{perfil.ToLowerInvariant()}";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, sufixo);
            var (email, _) = await CenarioAcessoPessoa.CriarContaAsync(_fabrica, cenario.IgrejaId, perfil, sufixo);
            var cliente = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, email);

            var resposta = await cliente.GetAsync("/api/usuarios");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        private async Task<string> LerSenhaHashAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Email == email)).SenhaHash;
        }

        private async Task<int> LerUsuarioIdAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Email == email)).Id;
        }
    }
}
