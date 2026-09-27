using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    public class RF28_DisponiveisTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF28_DisponiveisTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task PessoasDisponiveis_ComoProfessor_RetornaSoNomeESituacao()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf28a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/departamentos/{cenario.TurmaComAtribuicaoId}/pessoas-disponiveis");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var itens = json.RootElement.EnumerateArray().ToList();

            Assert.NotEmpty(itens);
            foreach (var item in itens)
            {
                var chaves = item.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
                Assert.Equal(new[] { "id", "nome", "situacao" }, chaves);
            }
        }

        [Fact]
        public async Task PessoasDisponiveis_ComoAdmin_MantemDtoCompleto()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf28b");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/departamentos/{cenario.TurmaComAtribuicaoId}/pessoas-disponiveis");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var primeiro = json.RootElement.EnumerateArray().First();

            Assert.True(primeiro.TryGetProperty("email", out _));
            Assert.True(primeiro.TryGetProperty("celular", out _));
        }

        [Fact]
        public async Task PessoasDisponiveis_ProfessorSemAtribuicaoNaTurma_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf28c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/departamentos/{cenario.TurmaSemAtribuicaoId}/pessoas-disponiveis");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }
    }
}
