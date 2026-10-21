using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Seguranca;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using KoinoniaHub.API.Dominio.Termos;

namespace KoinoniaHub.API.Tests
{
    // Convite de primeiro acesso como único caminho de definição e redefinição de
    // senha (RF39, RF40, RF15/CSU20): só o hash SHA-256 do token vai ao banco
    // (39.3), validade de 7 dias e um convite novo invalida o anterior (39.4/15.2),
    // uso único (40.1), convite expirado, já usado ou de conta inativa é recusado (40.2),
    // o token sai em claro só na geração (39.2), a senha é definida pelo próprio usuário
    // (15.3) e a senha atual vale até o uso (CSU20).
    public class RF39_RF40_ConviteTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string SenhaNova = "NovaSenha@456";
        private const string MensagemExpirado = "Este convite expirou. Solicite um novo link ao administrador.";
        private const string MensagemInvalido = "Convite inválido ou já utilizado. Solicite um novo link ao administrador.";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF39_RF40_ConviteTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task GerarConvite_GuardaSoHash()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf39a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var antes = DateTime.UtcNow;

            var (resposta, token) = await GerarConviteAsync(admin, cenario.AlvoId);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(token));

            var usuario = await LerUsuarioAsync(cenario.AlvoId);

            // RNF 39.3: no banco fica apenas o SHA-256 (64 caracteres hexadecimais) do token.
            Assert.Equal(ConviteTokenHelper.CalcularHash(token), usuario.ConviteTokenHash);
            Assert.NotEqual(token, usuario.ConviteTokenHash);
            Assert.Equal(64, usuario.ConviteTokenHash!.Length);
            Assert.DoesNotContain(token, usuario.ConviteTokenHash);

            // RNF 39.4: validade de 7 dias.
            Assert.NotNull(usuario.ConviteExpiraEm);
            Assert.InRange(usuario.ConviteExpiraEm!.Value, antes.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        }

        [Fact]
        public async Task GerarConvite_ComoProfessor_Retorna403()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf39b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PostAsync($"/api/usuarios/{cenario.AlvoId}/convite", content: null);

            // RNF 39.1 / 15.1: operação restrita ao Administrador.
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Null((await LerUsuarioAsync(cenario.AlvoId)).ConviteTokenHash);
        }

        [Fact]
        public async Task Ativar_ConviteExpirado_Retorna400()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf40a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);
            await ExpirarConviteAsync(cenario.AlvoId);

            var publico = _fabrica.CreateClient();
            var validar = await publico.GetAsync($"/api/auth/primeiro-acesso/{token}");
            var ativar = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });

            // RNF 40.2: convite expirado é recusado com mensagem clara, na validação e na ativação.
            Assert.Equal(HttpStatusCode.BadRequest, validar.StatusCode);
            Assert.Equal(MensagemExpirado, await LerMensagemAsync(validar));
            Assert.Equal(HttpStatusCode.BadRequest, ativar.StatusCode);
            Assert.Equal(MensagemExpirado, await LerMensagemAsync(ativar));

            // Nada mudou na conta: a senha anterior continua válida e o convite expirado não é consumido.
            Assert.Equal(HttpStatusCode.OK, await LoginAsync(cenario.EmailAlvo, CenarioAcessoPessoa.Senha));
            Assert.NotNull((await LerUsuarioAsync(cenario.AlvoId)).ConviteTokenHash);
        }

        [Fact]
        public async Task Ativar_SegundaVez_Retorna400()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf40b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);

            var publico = _fabrica.CreateClient();
            var primeira = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });
            Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);

            // RNF 40.1: após a definição da senha o token é descartado...
            var usuario = await LerUsuarioAsync(cenario.AlvoId);
            Assert.Null(usuario.ConviteTokenHash);
            Assert.Null(usuario.ConviteExpiraEm);

            // RNF 40.3: ...e a senha fica gravada apenas como hash BCrypt.
            Assert.StartsWith("$2", usuario.SenhaHash, StringComparison.Ordinal);
            Assert.DoesNotContain(SenhaNova, usuario.SenhaHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(SenhaNova, usuario.SenhaHash));

            // Segundo uso do mesmo link: recusado (40.1/40.2), sem alterar a senha definida.
            var segunda = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = "Outra@789", AceiteTermoVersao = TermosDeUso.Vigente.Versao });
            var validar = await publico.GetAsync($"/api/auth/primeiro-acesso/{token}");

            Assert.Equal(HttpStatusCode.BadRequest, segunda.StatusCode);
            Assert.Equal(MensagemInvalido, await LerMensagemAsync(segunda));
            Assert.Equal(HttpStatusCode.NotFound, validar.StatusCode);
            Assert.Equal(HttpStatusCode.OK, await LoginAsync(cenario.EmailAlvo, SenhaNova));
            Assert.Equal(HttpStatusCode.BadRequest, await LoginAsync(cenario.EmailAlvo, "Outra@789"));
        }

        [Fact]
        public async Task GerarNovoConvite_InvalidaAnterior()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf39c");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var (_, tokenA) = await GerarConviteAsync(admin, cenario.AlvoId);
            var (_, tokenB) = await GerarConviteAsync(admin, cenario.AlvoId);
            Assert.NotEqual(tokenA, tokenB);

            // RNF 39.4 / 15.2: só o hash do convite mais recente permanece no banco.
            Assert.Equal(ConviteTokenHelper.CalcularHash(tokenB), (await LerUsuarioAsync(cenario.AlvoId)).ConviteTokenHash);

            var publico = _fabrica.CreateClient();
            var validarA = await publico.GetAsync($"/api/auth/primeiro-acesso/{tokenA}");
            var ativarA = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = tokenA, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });
            var ativarB = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = tokenB, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.NotFound, validarA.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, ativarA.StatusCode);
            Assert.Equal(MensagemInvalido, await LerMensagemAsync(ativarA));
            Assert.Equal(HttpStatusCode.OK, ativarB.StatusCode);
        }

        [Fact]
        public async Task GerarConvite_SenhaAtualContinuaValidaAteOUso()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf15a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);

            // CSU20 / Plano 6.8: redefinir acesso não derruba a senha atual; ela vale até o convite ser usado.
            Assert.Equal(HttpStatusCode.OK, await LoginAsync(cenario.EmailAlvo, CenarioAcessoPessoa.Senha));
            Assert.True(await LerConvitePendenteAsync(admin, cenario.AlvoId));

            var ativar = await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });
            Assert.Equal(HttpStatusCode.OK, ativar.StatusCode);

            // Depois do uso: só a senha definida pela própria pessoa vale (RNF 15.3).
            Assert.Equal(HttpStatusCode.BadRequest, await LoginAsync(cenario.EmailAlvo, CenarioAcessoPessoa.Senha));
            Assert.Equal(HttpStatusCode.OK, await LoginAsync(cenario.EmailAlvo, SenhaNova));
            Assert.False(await LerConvitePendenteAsync(admin, cenario.AlvoId));
        }

        [Fact]
        public async Task ListarEObter_AposGerarConvite_NaoDevolvemToken()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf39d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);
            Assert.False(string.IsNullOrWhiteSpace(token));

            // RNF 39.2: o token sai em claro só na resposta da geração; as leituras não o devolvem.
            var lista = await admin.GetAsync("/api/usuarios");
            var item = await admin.GetAsync($"/api/usuarios/{cenario.AlvoId}");
            Assert.Equal(HttpStatusCode.OK, lista.StatusCode);
            Assert.Equal(HttpStatusCode.OK, item.StatusCode);

            var corpoLista = await lista.Content.ReadAsStringAsync();
            var corpoItem = await item.Content.ReadAsStringAsync();
            Assert.DoesNotContain(token, corpoLista);
            Assert.DoesNotContain(token, corpoItem);

            using var json = JsonDocument.Parse(corpoItem);
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("conviteToken").ValueKind);
            Assert.True(json.RootElement.GetProperty("convitePendente").GetBoolean());
        }

        [Fact]
        public async Task Inativar_DescartaConvitePendente_EReativarNaoORecria()
        {
            // Corretiva da 7.3 (RNF 40.2): ao inativar a conta pela API, o convite pendente é
            // descartado (hash e validade nulos); reativar não o traz de volta, então o link
            // antigo continua recusado e o Admin precisa gerar um novo.
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf40c");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);
            var hashAntes = (await LerUsuarioAsync(cenario.AlvoId)).SenhaHash;

            var inativar = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AlvoId}", new { Ativo = false });
            Assert.Equal(HttpStatusCode.NoContent, inativar.StatusCode);

            var inativo = await LerUsuarioAsync(cenario.AlvoId);
            Assert.False(inativo.Ativo);
            Assert.Null(inativo.ConviteTokenHash);
            Assert.Null(inativo.ConviteExpiraEm);
            Assert.False(await LerConvitePendenteAsync(admin, cenario.AlvoId)); // a tag "Convite pendente" some

            var reativar = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AlvoId}", new { Ativo = true });
            Assert.Equal(HttpStatusCode.NoContent, reativar.StatusCode);
            Assert.True((await LerUsuarioAsync(cenario.AlvoId)).Ativo);
            Assert.Null((await LerUsuarioAsync(cenario.AlvoId)).ConviteTokenHash);

            // O link antigo é recusado como inválido, na validação e na ativação.
            var publico = _fabrica.CreateClient();
            var validar = await publico.GetAsync($"/api/auth/primeiro-acesso/{token}");
            var ativar = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.NotFound, validar.StatusCode);
            Assert.Equal(MensagemInvalido, await LerMensagemAsync(validar));
            Assert.Equal(HttpStatusCode.BadRequest, ativar.StatusCode);
            Assert.Equal(MensagemInvalido, await LerMensagemAsync(ativar));
            Assert.Equal(hashAntes, (await LerUsuarioAsync(cenario.AlvoId)).SenhaHash);

            // Um convite novo, gerado pelo Admin, volta a funcionar.
            var (novo, tokenNovo) = await GerarConviteAsync(admin, cenario.AlvoId);
            Assert.Equal(HttpStatusCode.OK, novo.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await publico.GetAsync($"/api/auth/primeiro-acesso/{tokenNovo}")).StatusCode);
        }

        [Fact]
        public async Task Ativar_ContaInativadaForaDaApi_Retorna400()
        {
            // RNF 40.2, defesa em profundidade: se a conta ficar inativa por outro caminho
            // (sem passar pelo PATCH, que descarta o convite), o convite ainda é recusado
            // com a mensagem de conta inativa, e nada é consumido nem gravado.
            const string mensagemInativo = "Este usuário está inativo. Procure o administrador.";
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf40d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);
            var hashAntes = (await LerUsuarioAsync(cenario.AlvoId)).SenhaHash;
            await InativarNoBancoAsync(cenario.AlvoId);

            var publico = _fabrica.CreateClient();
            var validar = await publico.GetAsync($"/api/auth/primeiro-acesso/{token}");
            var ativar = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = SenhaNova, AceiteTermoVersao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.BadRequest, validar.StatusCode);
            Assert.Equal(mensagemInativo, await LerMensagemAsync(validar));
            Assert.Equal(HttpStatusCode.BadRequest, ativar.StatusCode);
            Assert.Equal(mensagemInativo, await LerMensagemAsync(ativar));

            var usuario = await LerUsuarioAsync(cenario.AlvoId);
            Assert.Equal(hashAntes, usuario.SenhaHash);
            Assert.NotNull(usuario.ConviteTokenHash);

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.False(await db.AceitesTermo.AnyAsync(a => a.UsuarioId == cenario.AlvoId));
        }

        [Fact]
        public async Task Reativar_ContaInativadaForaDaApi_DescartaConviteAntigo()
        {
            // Conta inativada por outro caminho ainda carrega o hash; reativá-la pelo PATCH
            // não pode reviver o link antigo (é o caso das contas inativadas antes da 7.3).
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf40e");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var (_, token) = await GerarConviteAsync(admin, cenario.AlvoId);
            await InativarNoBancoAsync(cenario.AlvoId);
            Assert.NotNull((await LerUsuarioAsync(cenario.AlvoId)).ConviteTokenHash);

            var reativar = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AlvoId}", new { Ativo = true });
            Assert.Equal(HttpStatusCode.NoContent, reativar.StatusCode);

            var usuario = await LerUsuarioAsync(cenario.AlvoId);
            Assert.True(usuario.Ativo);
            Assert.Null(usuario.ConviteTokenHash);
            Assert.Null(usuario.ConviteExpiraEm);
            Assert.Equal(HttpStatusCode.NotFound, (await _fabrica.CreateClient().GetAsync($"/api/auth/primeiro-acesso/{token}")).StatusCode);
        }

        [Fact]
        public async Task ResetarSenha_RotaRemovida_Retorna404()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf15b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var hashAntes = (await LerUsuarioAsync(cenario.AlvoId)).SenhaHash;

            // RNF 15.3: não existe mais caminho pelo qual o Administrador defina a senha de outro usuário.
            var resposta = await admin.PatchAsJsonAsync($"/api/usuarios/{cenario.AlvoId}/resetar-senha", new { NovaSenha = SenhaNova });

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(hashAntes, (await LerUsuarioAsync(cenario.AlvoId)).SenhaHash);
            Assert.Equal(HttpStatusCode.OK, await LoginAsync(cenario.EmailAlvo, CenarioAcessoPessoa.Senha));
        }

        // ---------- apoio ----------

        private static async Task<(HttpResponseMessage resposta, string token)> GerarConviteAsync(HttpClient admin, int usuarioId)
        {
            var resposta = await admin.PostAsync($"/api/usuarios/{usuarioId}/convite", content: null);
            if (resposta.StatusCode != HttpStatusCode.OK)
                return (resposta, string.Empty);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return (resposta, json.RootElement.GetProperty("token").GetString() ?? string.Empty);
        }

        private async Task<HttpStatusCode> LoginAsync(string email, string senha)
        {
            var cliente = _fabrica.CreateClient();
            var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = email, Senha = senha });
            return resposta.StatusCode;
        }

        private async Task<Usuario> LerUsuarioAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        }

        private async Task InativarNoBancoAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == usuarioId);
            usuario.Ativo = false;
            await db.SaveChangesAsync();
        }

        private async Task ExpirarConviteAsync(int usuarioId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var usuario = await db.Usuarios.SingleAsync(u => u.Id == usuarioId);
            usuario.ConviteExpiraEm = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        private static async Task<bool> LerConvitePendenteAsync(HttpClient admin, int usuarioId)
        {
            var resposta = await admin.GetAsync($"/api/usuarios/{usuarioId}");
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("convitePendente").GetBoolean();
        }

        private static async Task<string?> LerMensagemAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("mensagem").GetString();
        }
    }

    // Semente mínima: igreja com um Admin (que gera convites), um Professor (que não
    // pode) e a conta-alvo, vinculada a uma pessoa e com senha conhecida.
    public sealed class CenarioConvite
    {
        public int IgrejaId { get; init; }
        public int AlvoId { get; init; }
        public string EmailAdmin { get; init; } = string.Empty;
        public string EmailProfessor { get; init; } = string.Empty;
        public string EmailAlvo { get; init; } = string.Empty;

        public static async Task<CenarioConvite> CriarAsync(KoinoniaHubWebApplicationFactory fabrica, string sufixo)
        {
            using var escopo = fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var senhaHash = BCrypt.Net.BCrypt.HashPassword(CenarioAcessoPessoa.Senha);
            var igreja = new Igreja { Nome = $"Igreja {sufixo}" };

            var emailAdmin = $"admin.{sufixo}@teste.com";
            var emailProfessor = $"professor.{sufixo}@teste.com";
            var emailAlvo = $"alvo.{sufixo}@teste.com";

            var alvoPessoa = new Pessoa { Nome = $"Pessoa Alvo {sufixo}", Igreja = igreja, Email = emailAlvo };
            var alvo = new Usuario { Email = emailAlvo, SenhaHash = senhaHash, Perfil = "Usuario", Igreja = igreja, Pessoa = alvoPessoa };
            var admin = new Usuario { Email = emailAdmin, SenhaHash = senhaHash, Perfil = "Admin", Igreja = igreja };
            var professor = new Usuario { Email = emailProfessor, SenhaHash = senhaHash, Perfil = "Professor", Igreja = igreja };

            // Admin e Professor com aceite (operam rotas autenticadas); o alvo é uma conta
            // nova aguardando ativação, sem aceite — o primeiro acesso é que o registra.
            db.AddRange(admin, professor, alvo);
            db.AddRange(SementeTermo.AceiteDe(admin), SementeTermo.AceiteDe(professor));

            await db.SaveChangesAsync();

            return new CenarioConvite
            {
                IgrejaId = igreja.Id,
                AlvoId = alvo.Id,
                EmailAdmin = emailAdmin,
                EmailProfessor = emailProfessor,
                EmailAlvo = emailAlvo
            };
        }
    }
}
