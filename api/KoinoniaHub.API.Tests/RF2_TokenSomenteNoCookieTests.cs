using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;
using KoinoniaHub.API.Dominio.Termos;

namespace KoinoniaHub.API.Tests
{
    // Monografia, seção 4.8 / RF2: o JWT é entregue ao navegador em cookie httpOnly,
    // "não acessível a scripts em execução na página". Logo, nem o cadastro inicial
    // nem o login podem devolver o token também no corpo JSON.
    public class RF2_TokenSomenteNoCookieTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string NomeCookie = "kh_token";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF2_TokenSomenteNoCookieTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task RegistrarAdmin_NaoDevolveTokenNoCorpo_SoNoCookie()
        {
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar-admin", new
            {
                Igreja = new { Nome = "Igreja Cookie" },
                EmailAdmin = "admin.cookie@teste.com",
                SenhaAdmin = CenarioAcessoPessoa.Senha,
                NomeAdmin = "Admin Cookie",
                AceiteTermoVersao = TermosDeUso.Vigente.Versao
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            await VerificarTokenSoNoCookieAsync(resposta, "usuarioId", "nomeIgreja");
        }

        [Fact]
        public async Task Login_NaoDevolveTokenNoCorpo_SoNoCookie()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf2cookie");
            var cliente = _fabrica.CreateClient();

            var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = cenario.EmailAdmin, Senha = CenarioAcessoPessoa.Senha });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            await VerificarTokenSoNoCookieAsync(resposta, "usuarioId", "perfil");

            // A sessão estabelecida só pelo cookie é suficiente para uma rota autenticada.
            var meusDados = await cliente.GetAsync("/api/meus-dados");
            Assert.Equal(HttpStatusCode.OK, meusDados.StatusCode);
        }

        private static async Task VerificarTokenSoNoCookieAsync(HttpResponseMessage resposta, params string[] camposEsperados)
        {
            Assert.True(resposta.Headers.TryGetValues("Set-Cookie", out var cookies));
            var cookie = Assert.Single(cookies!, c => c.StartsWith($"{NomeCookie}=", StringComparison.Ordinal));
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);

            var jwt = cookie.Substring(NomeCookie.Length + 1).Split(';')[0];
            Assert.False(string.IsNullOrWhiteSpace(jwt));

            var corpo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(corpo);

            // O corpo continua trazendo os dados de sessão que o front usa...
            foreach (var campo in camposEsperados)
                Assert.True(json.RootElement.TryGetProperty(campo, out _), $"Campo '{campo}' ausente no corpo.");

            // ...mas nenhuma propriedade chamada "token" e, principalmente, nenhuma cópia do JWT.
            Assert.DoesNotContain(json.RootElement.EnumerateObject(),
                p => p.Name.Equals("token", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(jwt, corpo);
        }
    }
}
