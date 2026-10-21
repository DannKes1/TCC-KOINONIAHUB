using Microsoft.AspNetCore.Hosting;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // A mesma API em memória, informando a porta https (a do perfil https do
    // launchSettings.json). O TestServer não abre porta nenhuma; sem esta chave o
    // UseHttpsRedirection não sabe para onde redirecionar e deixa passar.
    public class FabricaComPortaHttps : KoinoniaHubWebApplicationFactory
    {
        public const int PortaHttps = 7054;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("https_port", PortaHttps.ToString());
        }
    }
}
