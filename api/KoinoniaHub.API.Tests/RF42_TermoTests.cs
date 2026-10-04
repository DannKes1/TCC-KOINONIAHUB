using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Aplicacao.Servicos.Implementacoes;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // Termo de Uso e Sigilo (RF42 / RF43; RNFs 1.5, 2.5, 13.4, 40.5, 42.1–42.8; CSU23).
    public class RF42_TermoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF42_TermoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        // ---------- versões (RNFs 42.2, 42.6, 42.7) ----------

        [Fact]
        public void TermosDeUso_VigenteTemVersaoTextoEHashSha256()
        {
            var vigente = TermosDeUso.Vigente;

            Assert.Equal("1.0", vigente.Versao);
            Assert.True(vigente.VigenteDesde <= DateTime.UtcNow);
            Assert.Contains("TERMO DE USO E SIGILO DO KOINONIAHUB", vigente.Texto);
            Assert.Equal(64, vigente.Hash.Length);
            Assert.Equal(TermosDeUso.CalcularHash(vigente.Texto), vigente.Hash);

            // RF42: o texto cobre dados tratados/finalidades, uso exclusivo na EBD, permissões do perfil,
            // proibição de copiar/compartilhar/divulgar e o registro do aceite.
            Assert.Contains("Escola Bíblica Dominical", vigente.Texto);
            Assert.Contains("perfil", vigente.Texto);
            Assert.Contains("não copiar, compartilhar, divulgar", vigente.Texto);
            Assert.Contains("O aceite deste Termo é registrado", vigente.Texto);

            // RNF 42.7: nada da igreja no texto fixo (vai dinamicamente na tela).
            Assert.DoesNotContain("Igreja Presbiteriana", vigente.Texto);
            Assert.DoesNotContain("Ji-Paraná", vigente.Texto);
        }

        [Fact]
        public void TermosDeUso_HashIndependeDaQuebraDeLinha()
        {
            var lf = new VersaoTermo("x", DateTime.UtcNow, "linha 1\nlinha 2\n");
            var crlf = new VersaoTermo("x", DateTime.UtcNow, "linha 1\r\nlinha 2\r\n");

            Assert.Equal(lf.Hash, crlf.Hash);
            Assert.Equal(lf.Texto, crlf.Texto);
        }

        [Fact]
        public void TermosDeUso_VersoesSaoUnicasEOrdenadas()
        {
            Assert.Equal(TermosDeUso.Versoes.Count, TermosDeUso.Versoes.Select(v => v.Versao).Distinct().Count());
            Assert.Null(TermosDeUso.ObterPorVersao("9.9"));
            Assert.NotNull(TermosDeUso.ObterPorVersao("1.0"));
            Assert.False(TermosDeUso.EhVigente(null));
            Assert.False(TermosDeUso.EhVigente(" "));
            Assert.True(TermosDeUso.EhVigente(" 1.0 "));
        }

        // ---------- GET /api/termo/vigente ----------

        [Fact]
        public async Task Vigente_Publico_RetornaTextoHashESemIgreja()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.GetAsync("/api/termo/vigente");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;
            Assert.Equal(TermosDeUso.Vigente.Versao, raiz.GetProperty("versao").GetString());
            Assert.Equal(TermosDeUso.Vigente.Hash, raiz.GetProperty("hash").GetString());
            Assert.Equal(TermosDeUso.Vigente.Texto, raiz.GetProperty("texto").GetString());
            Assert.Equal(JsonValueKind.Null, raiz.GetProperty("igreja").ValueKind);
        }

        [Fact]
        public async Task Vigente_Autenticado_RetornaIgrejaDoUsuario()
        {
            // Conta pendente: o endpoint do termo é alcançável mesmo sem aceite (PermitirSemAceiteTermo).
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf42vig", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync("/api/termo/vigente");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("Igreja rf42vig", json.RootElement.GetProperty("igreja").GetProperty("nome").GetString());
        }

        [Fact]
        public async Task Vigente_ComTokenDeConvite_RetornaIgrejaDoConvite()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf42tok");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var convite = await admin.PostAsync($"/api/usuarios/{cenario.AlvoId}/convite", content: null);
            using var jsonConvite = JsonDocument.Parse(await convite.Content.ReadAsStringAsync());
            var token = jsonConvite.RootElement.GetProperty("token").GetString();

            var publico = _fabrica.CreateClient();
            var resposta = await publico.GetAsync($"/api/termo/vigente?token={Uri.EscapeDataString(token!)}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal("Igreja rf42tok", json.RootElement.GetProperty("igreja").GetProperty("nome").GetString());
        }

        // ---------- login + aceitar (RNFs 2.5, 13.4, 42.1, 42.2, 42.5) ----------

        [Fact]
        public async Task Login_SemAceiteVigente_RetornaTermoPendente()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf42a", aceitarTermo: false);
            var cliente = _fabrica.CreateClient();

            var login = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailProfessor, Senha = CenarioAcessoPessoa.Senha });

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("termoPendente").GetBoolean());

            // A sessão existe (o gate é a tela do termo); o próprio termo é consultável com ela.
            var vigente = await cliente.GetAsync("/api/termo/vigente");
            Assert.Equal(HttpStatusCode.OK, vigente.StatusCode);
        }

        [Fact]
        public async Task Aceitar_VersaoDiferenteDaVigente_Retorna400()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf42b", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = "0.9" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Contains("não é a vigente", json.RootElement.GetProperty("mensagem").GetString());
            Assert.Equal(0, await ContarAceitesAsync(cenario.EmailProfessor));
        }

        [Fact]
        public async Task Aceitar_GravaHashDaVersaoEMeio()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf42c", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var antes = DateTime.UtcNow.AddSeconds(-1);

            var resposta = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(TermosDeUso.Vigente.Versao, json.RootElement.GetProperty("versao").GetString());
            Assert.Equal(MeioAceiteTermo.Login, json.RootElement.GetProperty("meio").GetString());
            Assert.True(json.RootElement.GetProperty("vigente").GetBoolean());

            var aceite = await LerAceiteAsync(cenario.EmailProfessor);
            Assert.Equal(TermosDeUso.Vigente.Versao, aceite.TermoVersao);
            Assert.Equal(TermosDeUso.Vigente.Hash, aceite.TermoHash);          // RNF 42.2
            Assert.Equal(MeioAceiteTermo.Login, aceite.Meio);                  // RNF 42.5
            Assert.Equal(cenario.IgrejaId, aceite.IgrejaId);                   // RNF 42.3
            Assert.InRange(aceite.AceitoEm, antes, DateTime.UtcNow.AddSeconds(1));

            // Depois do aceite, o login deixa de sinalizar pendência.
            var login = await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailProfessor, Senha = CenarioAcessoPessoa.Senha });
            using var jsonLogin = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.False(jsonLogin.RootElement.GetProperty("termoPendente").GetBoolean());
        }

        [Fact]
        public async Task Aceitar_SegundaVezDaMesmaVersao_NaoDuplicaRegistro()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf42d", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var primeira = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });
            var segunda = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
            Assert.Equal(1, await ContarAceitesAsync(cenario.EmailProfessor));
        }

        [Fact]
        public async Task Aceitar_SemSessao_Retorna401()
        {
            var resposta = await _fabrica.CreateClient().PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        // ---------- cadastro inicial (RNF 1.5) ----------

        [Fact]
        public async Task RegistrarAdmin_SemAceite_Retorna400()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar-admin", new
            {
                Igreja = new { Nome = "Igreja Sem Aceite" },
                EmailAdmin = "admin.semaceite@teste.com",
                SenhaAdmin = CenarioAcessoPessoa.Senha,
                NomeAdmin = "Admin Sem Aceite"
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(AceiteTermoServico.MensagemAceiteObrigatorio, json.RootElement.GetProperty("mensagem").GetString());

            // Nada foi criado: nem igreja, nem usuário.
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.False(await db.Igrejas.AnyAsync(i => i.Nome == "Igreja Sem Aceite"));
            Assert.False(await db.Usuarios.AnyAsync(u => u.Email == "admin.semaceite@teste.com"));
        }

        [Fact]
        public async Task RegistrarAdmin_ComAceite_GravaMeioCadastroInicialESemPendencia()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar-admin", new
            {
                Igreja = new { Nome = "Igreja Com Aceite" },
                EmailAdmin = "admin.comaceite@teste.com",
                SenhaAdmin = CenarioAcessoPessoa.Senha,
                NomeAdmin = "Admin Com Aceite",
                AceiteTermoVersao = TermosDeUso.Vigente.Versao
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("termoPendente").GetBoolean());

            var aceite = await LerAceiteAsync("admin.comaceite@teste.com");
            Assert.Equal(MeioAceiteTermo.CadastroInicial, aceite.Meio);
            Assert.Equal(TermosDeUso.Vigente.Hash, aceite.TermoHash);
            Assert.Equal(json.RootElement.GetProperty("igrejaId").GetInt32(), aceite.IgrejaId);
        }

        // ---------- primeiro acesso (RNF 40.5) ----------

        [Fact]
        public async Task PrimeiroAcesso_SemAceite_Retorna400ENaoConsomeConvite()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf42pa");
            var token = await GerarConviteAsync(cenario);

            var publico = _fabrica.CreateClient();
            var resposta = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new { Token = token, NovaSenha = "NovaSenha@456" });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(AceiteTermoServico.MensagemAceiteObrigatorio, json.RootElement.GetProperty("mensagem").GetString());

            // Convite intacto e senha antiga válida: a pessoa pode voltar e aceitar.
            var validar = await publico.GetAsync($"/api/auth/primeiro-acesso/{token}");
            Assert.Equal(HttpStatusCode.OK, validar.StatusCode);
            Assert.Equal(0, await ContarAceitesAsync(cenario.EmailAlvo));
        }

        [Fact]
        public async Task PrimeiroAcesso_ComAceite_GravaMeioPrimeiroAcesso()
        {
            var cenario = await CenarioConvite.CriarAsync(_fabrica, "rf42pb");
            var token = await GerarConviteAsync(cenario);

            var publico = _fabrica.CreateClient();
            var resposta = await publico.PostAsJsonAsync("/api/auth/primeiro-acesso", new
            {
                Token = token,
                NovaSenha = "NovaSenha@456",
                AceiteTermoVersao = TermosDeUso.Vigente.Versao
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            var aceite = await LerAceiteAsync(cenario.EmailAlvo);
            Assert.Equal(MeioAceiteTermo.PrimeiroAcesso, aceite.Meio);
            Assert.Equal(TermosDeUso.Vigente.Hash, aceite.TermoHash);

            // Login com a nova senha já sem pendência.
            var login = await _fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailAlvo, Senha = "NovaSenha@456" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var jsonLogin = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.False(jsonLogin.RootElement.GetProperty("termoPendente").GetBoolean());
        }

        // ---------- RF43 ----------

        [Fact]
        public async Task MeusDados_EUsuarios_IncluemAceite()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf43a", aceitarTermo: false);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // Pendente: o filtro global ainda bloqueia Meus Dados (RF2_GateTermoTests cobre o detalhe).
            Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/meus-dados")).StatusCode);

            var aceitar = await admin.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });
            Assert.Equal(HttpStatusCode.OK, aceitar.StatusCode);

            using (var json = JsonDocument.Parse(await (await admin.GetAsync("/api/meus-dados")).Content.ReadAsStringAsync()))
            {
                var aceite = json.RootElement.GetProperty("aceiteTermo");
                Assert.Equal(TermosDeUso.Vigente.Versao, aceite.GetProperty("versao").GetString());
                Assert.True(aceite.GetProperty("vigente").GetBoolean());
                Assert.Equal($"Admin rf43a", json.RootElement.GetProperty("nome").GetString());
            }

            using (var json = JsonDocument.Parse(await (await admin.GetAsync("/api/usuarios")).Content.ReadAsStringAsync()))
            {
                var itens = json.RootElement.EnumerateArray().ToList();
                var doAdmin = itens.Single(u => u.GetProperty("email").GetString() == cenario.EmailAdmin);
                var doProfessor = itens.Single(u => u.GetProperty("email").GetString() == cenario.EmailProfessor);

                Assert.Equal(TermosDeUso.Vigente.Versao, doAdmin.GetProperty("aceiteTermo").GetProperty("versao").GetString());
                Assert.Equal(JsonValueKind.Null, doProfessor.GetProperty("aceiteTermo").ValueKind);
            }
        }

        // ---------- RNF 42.8 (unitário: o TestServer não informa IP de origem) ----------

        [Fact]
        public async Task Servico_RegistrarGuardaIpSoParaAuditoria()
        {
            using var ctx = new ContextoDeTeste();
            var igreja = new Igreja { Nome = "Igreja RF42" };
            var usuario = new Usuario { Email = "ip@teste.com", SenhaHash = "hash", Perfil = "Usuario", Igreja = igreja };
            ctx.Db.Add(usuario);
            await ctx.Db.SaveChangesAsync();

            var servico = new AceiteTermoServico(ctx.Db);
            await servico.RegistrarAsync(usuario.Id, igreja.Id, TermosDeUso.Vigente.Versao, "203.0.113.5");

            using var leitura = ctx.NovoContexto();
            var aceite = await leitura.AceitesTermo.SingleAsync(a => a.UsuarioId == usuario.Id);
            Assert.Equal("203.0.113.5", aceite.Ip);
            Assert.Equal(MeioAceiteTermo.Login, aceite.Meio);

            // O IP não sai em nenhum DTO (só auditoria).
            var resumo = await servico.ObterUltimoAceiteAsync(usuario.Id);
            Assert.NotNull(resumo);
            Assert.DoesNotContain("Ip", typeof(KoinoniaHub.API.Aplicacao.DTOs.Respostas.AceiteTermoRespostaDto).GetProperties().Select(p => p.Name));
        }

        // ---------- apoio ----------

        private async Task<string> GerarConviteAsync(CenarioConvite cenario)
        {
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var resposta = await admin.PostAsync($"/api/usuarios/{cenario.AlvoId}/convite", content: null);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("token").GetString()!;
        }

        private async Task<int> ContarAceitesAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.AceitesTermo.CountAsync(a => a.Usuario.Email == email);
        }

        private async Task<AceiteTermo> LerAceiteAsync(string email)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.AceitesTermo.AsNoTracking().SingleAsync(a => a.Usuario.Email == email);
        }
    }
}
