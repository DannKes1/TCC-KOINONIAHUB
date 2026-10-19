using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KoinoniaHub.API.Tests.Infraestrutura
{
    // A mesma API em memória, com o TimeProvider fixo em `Agora` (corretiva da 7.2:
    // dia civil da igreja). Só o RelogioIgreja lê o TimeProvider; o resto da API
    // (token, convite, CriadoEm) continua no relógio real.
    public class FabricaComRelogioFixo : KoinoniaHubWebApplicationFactory
    {
        // 18/10/2026 às 21h em Porto Velho (UTC−4) = 19/10/2026 01h UTC: o dia UTC já
        // virou, o dia civil da igreja ainda é 18.
        public static readonly DateTimeOffset Agora = new(2026, 10, 19, 1, 0, 0, TimeSpan.Zero);
        public static readonly DateOnly Hoje = new(2026, 10, 18);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new RelogioFixo(Agora));
            });
        }
    }

    public sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset _agora;

        public RelogioFixo(DateTimeOffset agora) => _agora = agora;

        public override DateTimeOffset GetUtcNow() => _agora;
    }
}
