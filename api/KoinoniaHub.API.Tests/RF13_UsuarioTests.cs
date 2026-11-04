using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Regras;
using KoinoniaHub.API.Aplicacao.Seguranca;
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
    // (15.1/39.1) já é coberto por RF39_RF40_ConviteTests. Corretiva G2 (RNF 13.5, RF14): um
    // e-mail por pessoa — a conta preenche o cadastro, a recusa cruzada na igreja e a troca do
    // e-mail de acesso pelo Administrador.
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

            // RNFs 13.1/14.1/16.1: a operação é restrita ao perfil Administrador, e o 403
            // traz { mensagem } como o resto da API (RespostaAutorizacaoHandler, 7.3)…
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(RespostaAutorizacaoHandler.MensagemSemPermissao, await LerMensagemAsync(resposta));

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
            Assert.Equal(RespostaAutorizacaoHandler.MensagemSemPermissao, await LerMensagemAsync(resposta));
        }

        // ---- Corretiva G2: um e-mail por pessoa (RNF 13.5) ----

        [Fact]
        public async Task Criar_PessoaSemEmail_CadastroRecebeOEmailDaConta()
        {
            const string email = "conta.rf13d@teste.com";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf13d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync("/api/usuarios", new { PessoaId = cenario.AlunoForaId, Email = email, Perfil = "Usuario" });
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            // No banco e na leitura da pessoa: o mesmo e-mail, com possuiConta.
            Assert.Equal(email, await LerEmailDaPessoaAsync(cenario.AlunoForaId));
            using var json = JsonDocument.Parse(await (await admin.GetAsync($"/api/pessoas/{cenario.AlunoForaId}")).Content.ReadAsStringAsync());
            Assert.Equal(email, json.RootElement.GetProperty("email").GetString());
            Assert.True(json.RootElement.GetProperty("possuiConta").GetBoolean());
        }

        [Fact]
        public async Task Criar_PessoaComOutroEmail_CadastroPassaAoEmailDaConta()
        {
            // Aluno Dentro nasce com e-mail próprio; a conta é criada com outro (o diálogo vem
            // preenchido com o da pessoa, mas é editável): o cadastro passa ao da conta.
            const string email = "conta.rf13e@teste.com";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf13e");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            Assert.Equal("aluno.dentro.rf13e@teste.com", await LerEmailDaPessoaAsync(cenario.AlunoDentroId));

            var resposta = await admin.PostAsJsonAsync("/api/usuarios", new { PessoaId = cenario.AlunoDentroId, Email = email, Perfil = "Usuario" });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(email, await LerEmailDaPessoaAsync(cenario.AlunoDentroId));
        }

        [Fact]
        public async Task Criar_EmailDeOutraPessoa_Retorna400ENaoCria()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf13f");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // Cadastro de outra pessoa (Aluno Dentro) e conta de outra pessoa (Professor).
            foreach (var email in new[] { "aluno.dentro.rf13f@teste.com", cenario.EmailProfessor })
            {
                var resposta = await admin.PostAsJsonAsync("/api/usuarios", new { PessoaId = cenario.AlunoForaId, Email = email, Perfil = "Usuario" });

                Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
                Assert.Equal(EmailDaPessoa.MensagemEmUso, await LerMensagemAsync(resposta));
            }

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.False(await db.Usuarios.AnyAsync(u => u.PessoaId == cenario.AlunoForaId));
        }

        [Fact]
        public async Task AtualizarEmail_AlteraOLoginSincronizaOCadastroEInvalidaOConvite()
        {
            // RF14 (Corretiva G2): o Administrador troca o e-mail de acesso; a pessoa passa a
            // entrar com o novo, o cadastro recebe o mesmo e o convite pendente deixa de valer.
            const string novo = "alvo.novo.rf14g@teste.com";
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf14g");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/usuarios/{cenario.AlvoId}/convite", content: null)).StatusCode);

            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AlvoId}", new { Email = novo });
            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

            using (var escopo = _fabrica.Services.CreateScope())
            {
                var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
                var usuario = await db.Usuarios.AsNoTracking().Include(u => u.Pessoa).SingleAsync(u => u.Id == cenario.AlvoId);
                Assert.Equal(novo, usuario.Email);
                Assert.Equal(novo, usuario.Pessoa!.Email);
                Assert.Null(usuario.ConviteTokenHash);
                Assert.Null(usuario.ConviteExpiraEm);
            }

            // A senha é a mesma: entra com o e-mail novo e não entra mais com o antigo.
            Assert.Equal(HttpStatusCode.OK, (await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = novo, Senha = CenarioAcessoPessoa.Senha })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailAlvo, Senha = CenarioAcessoPessoa.Senha })).StatusCode);
        }

        [Fact]
        public async Task AtualizarEmail_EmUso_Retorna400ENadaMuda()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14h");
            var outraIgreja = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14h2");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var professorId = await LerUsuarioIdAsync(cenario.EmailProfessor);

            // Cadastro de outra pessoa e conta de outra pessoa da igreja: recusa cruzada;
            // conta de outra igreja: regra global, inalterada (c).
            foreach (var (email, mensagem) in new[]
            {
                ("aluno.dentro.rf14h@teste.com", EmailDaPessoa.MensagemEmUso),
                (cenario.EmailUsuarioComum, EmailDaPessoa.MensagemEmUso),
                (outraIgreja.EmailAdmin, "Já existe um usuário com este e-mail.")
            })
            {
                var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Email = email, Perfil = "Pastor" });

                Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
                Assert.Equal(mensagem, await LerMensagemAsync(resposta));
            }

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var professor = await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == professorId);
            Assert.Equal(cenario.EmailProfessor, professor.Email);
            Assert.Equal("Professor", professor.Perfil); // nada da requisição recusada entrou
        }

        [Fact]
        public async Task AtualizarEmail_ComoProfessor_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf14i");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var professorId = await LerUsuarioIdAsync(cenario.EmailProfessor);

            var resposta = await professor.PatchAsJsonAsync($"/api/usuarios/{professorId}", new { Email = "professor.novo.rf14i@teste.com" });

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(RespostaAutorizacaoHandler.MensagemSemPermissao, await LerMensagemAsync(resposta));
            Assert.Equal(cenario.EmailProfessor, await LerEmailDoUsuarioAsync(professorId));
        }

        private async Task<string?> LerEmailDaPessoaAsync(int pessoaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.Pessoas.AsNoTracking().SingleAsync(p => p.Id == pessoaId)).Email;
        }

        private async Task<string> LerEmailDoUsuarioAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId)).Email;
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
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
