using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF19 / CSU17 — listagem de turmas. Desde o ajuste B1 o menu do Professor não tem
    // Turmas EBD, mas a rota continua aberta por endereço e a API é que decide o que ele
    // vê: o Professor recebe só as turmas em que tem atribuição ativa; a gestão recebe
    // todas as turmas da igreja.
    public class RF19_ListarTurmasTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF19_ListarTurmasTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task ListarTurmas_Professor_RecebeSoAsTurmasComAtribuicaoAtiva()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf19a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var doProfessor = await IdsDasTurmasAsync(professor);
            var doAdmin = await IdsDasTurmasAsync(admin);

            Assert.Equal(new[] { cenario.TurmaComAtribuicaoId }, doProfessor);
            Assert.Equal(
                new[] { cenario.TurmaComAtribuicaoId, cenario.TurmaSemAtribuicaoId }.OrderBy(id => id),
                doAdmin.OrderBy(id => id));
        }

        private static async Task<List<int>> IdsDasTurmasAsync(HttpClient cliente)
        {
            var resposta = await cliente.GetAsync("/api/departamentos");
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToList();
        }
    }
}
