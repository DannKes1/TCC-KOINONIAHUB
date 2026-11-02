using System.Net;
using System.Net.Http.Json;
using KoinoniaHub.API.Dominio.Termos;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // API atrás de um proxy reverso que termina o TLS (implantação, Entrega F): com
    // ProxyReverso:ConfiarNaRedeInterna, X-Forwarded-Proto mantém o redirecionamento HTTPS
    // (RNF 2.3) sem laço e X-Forwarded-For é o IP gravado no aceite do termo (RNF 42.8).
    // Sem a chave só proxies de loopback são aceitos (padrão do ASP.NET); como a conexão do
    // TestServer conta como loopback, o caso "proxy não confiável" não é demonstrável aqui.
    // Sem os cabeçalhos a API se comporta como antes (RF2_HttpsTests).
    public class RF2_ProxyReversoTests : IClassFixture<FabricaAtrasDeProxy>
    {
        private const string IpDoNavegador = "203.0.113.7";

        private readonly FabricaAtrasDeProxy _atrasDeProxy;

        public RF2_ProxyReversoTests(FabricaAtrasDeProxy atrasDeProxy)
        {
            _atrasDeProxy = atrasDeProxy;
        }

        [Fact]
        public async Task Http_ComForwardedProtoHttps_NaoRedireciona()
        {
            var cliente = ClienteHttp(_atrasDeProxy);
            cliente.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

            var resposta = await cliente.GetAsync("/api/termo/vigente");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task AceitarTermo_GravaIpDoForwardedFor()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_atrasDeProxy, "rf2proxy", aceitarTermo: false);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_atrasDeProxy, cenario.EmailProfessor);
            professor.DefaultRequestHeaders.Add("X-Forwarded-For", IpDoNavegador);

            var resposta = await professor.PostAsJsonAsync("/api/termo/aceitar", new { Versao = TermosDeUso.Vigente.Versao });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var escopo = _atrasDeProxy.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            var aceite = await db.AceitesTermo
                .Where(a => a.Usuario.Email == cenario.EmailProfessor)
                .OrderByDescending(a => a.Id)
                .FirstAsync();

            Assert.Equal(IpDoNavegador, aceite.Ip);
        }

        private static HttpClient ClienteHttp(KoinoniaHubWebApplicationFactory fabrica) =>
            fabrica.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = false
            });
    }
}
