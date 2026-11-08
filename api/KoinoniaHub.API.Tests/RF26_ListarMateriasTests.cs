using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    // RF26 (RNF 26.1): listar as matérias da turma exige perfil administrativo (Admin, Pastor
    // ou Superintendente) ou atribuição ativa na turma (Professor ou Auxiliar). O Professor sem
    // atribuição na turma e a conta de perfil Usuário, mesmo com atribuição, recebem 403
    // (RNF 21.4: o perfil define a área; a atribuição, as turmas).
    public class RF26_ListarMateriasTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF26_ListarMateriasTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Gestao_ListaAsMateriasDeQualquerTurmaDaIgreja()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf26a");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/materias?departamentoId={cenario.TurmaSemAtribuicaoId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(["Materia B rf26a"], await NomesAsync(resposta));
        }

        [Fact]
        public async Task ProfessorComAtribuicaoAtiva_ListaAsMateriasDaTurma()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf26b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/materias?departamentoId={cenario.TurmaComAtribuicaoId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal(["Materia A rf26b"], await NomesAsync(resposta));
        }

        [Fact]
        public async Task ProfessorSemAtribuicaoEPerfilUsuario_Recebem403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf26c");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            // Conta de perfil Usuário com atribuição ativa na turma: o perfil barra mesmo assim.
            var (emailUsuario, _) = await CenarioAcessoPessoa.CriarContaAsync(
                _fabrica, cenario.IgrejaId, "Usuario", "rf26c2", cenario.TurmaComAtribuicaoId);
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, emailUsuario);

            var semAtribuicao = await professor.GetAsync($"/api/materias?departamentoId={cenario.TurmaSemAtribuicaoId}");
            var perfilUsuario = await usuario.GetAsync($"/api/materias?departamentoId={cenario.TurmaComAtribuicaoId}");

            Assert.Equal(HttpStatusCode.Forbidden, semAtribuicao.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, perfilUsuario.StatusCode);
        }

        private static async Task<string[]> NomesAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.EnumerateArray().Select(m => m.GetProperty("nome").GetString()!).ToArray();
        }
    }
}
