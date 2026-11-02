using Microsoft.AspNetCore.Hosting;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // A mesma API em memória configurada como na implantação (Entrega F): porta https
    // conhecida e ProxyReverso:ConfiarNaRedeInterna=true, para os cabeçalhos
    // X-Forwarded-Proto e X-Forwarded-For do proxy serem aplicados.
    public class FabricaAtrasDeProxy : FabricaComPortaHttps
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ProxyReverso:ConfiarNaRedeInterna", "true");
        }
    }
}
