using System.Net;
using System.Net.Http.Json;
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

        // CSU12 FA2, primeira frase: pessoa inativa não é oferecida entre as disponíveis para
        // matrícula, nem na lista reduzida do Professor nem na completa do Admin (a recusa da
        // matrícula pela API, RNF 9.4, está em RF9_PessoaInativaSemAcessoTests). O Aluno Fora
        // não tem matrícula na turma A; inativado pela própria API (PUT /api/pessoas), só a
        // situação do cadastro o tira da lista, e as outras pessoas ativas continuam nela.
        [Fact]
        public async Task PessoasDisponiveis_PessoaInativa_NaoApareceEntreAsDisponiveis()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf28d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var rota = $"/api/departamentos/{cenario.TurmaComAtribuicaoId}/pessoas-disponiveis";

            Assert.Contains(cenario.AlunoForaId, await IdsDisponiveisAsync(professor, rota));
            Assert.Contains(cenario.AlunoForaId, await IdsDisponiveisAsync(admin, rota));

            var inativar = await admin.PutAsJsonAsync($"/api/pessoas/{cenario.AlunoForaId}", new { Nome = "Aluno Fora rf28d", Situacao = "Inativo" });
            Assert.Equal(HttpStatusCode.NoContent, inativar.StatusCode);

            var paraOProfessor = await IdsDisponiveisAsync(professor, rota);
            var paraOAdmin = await IdsDisponiveisAsync(admin, rota);

            Assert.DoesNotContain(cenario.AlunoForaId, paraOProfessor);
            Assert.DoesNotContain(cenario.AlunoForaId, paraOAdmin);
            Assert.Contains(cenario.UsuarioComumPessoaId, paraOProfessor);
            Assert.Contains(cenario.UsuarioComumPessoaId, paraOAdmin);
        }

        private static async Task<List<int>> IdsDisponiveisAsync(HttpClient cliente, string rota)
        {
            var resposta = await cliente.GetAsync(rota);
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToList();
        }
    }
}
