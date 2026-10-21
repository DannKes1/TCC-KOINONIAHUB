using System.Net;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KoinoniaHub.API.Tests
{
    // Comunicação sobre HTTPS (RNFs 1.4, 2.3, 40.4, 42.4, 44.4): uma requisição em http é
    // redirecionada para o mesmo caminho em https (UseHttpsRedirection, Program.cs). O cookie
    // httpOnly + Secure da sessão já é conferido em RF2_TokenSomenteNoCookieTests.
    public class RF2_HttpsTests : IClassFixture<FabricaComPortaHttps>
    {
        private readonly FabricaComPortaHttps _fabrica;

        public RF2_HttpsTests(FabricaComPortaHttps fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task RequisicaoHttp_RedirecionaParaHttps_Retorna307()
        {
            var http = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = false
            });

            var resposta = await http.GetAsync("/api/termo/vigente");

            Assert.Equal(HttpStatusCode.TemporaryRedirect, resposta.StatusCode);
            Assert.Equal(
                new Uri($"https://localhost:{FabricaComPortaHttps.PortaHttps}/api/termo/vigente"),
                resposta.Headers.Location);

            // Controle: a mesma rota em https responde normalmente.
            var https = await _fabrica.CreateClient().GetAsync("/api/termo/vigente");
            Assert.Equal(HttpStatusCode.OK, https.StatusCode);
        }
    }
}
