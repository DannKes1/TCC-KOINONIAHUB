using System.Net;
using System.Text.Json;
using KoinoniaHub.API.Tests.Infraestrutura;

namespace KoinoniaHub.API.Tests
{
    public class RF11_PessoaAcessoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF11_PessoaAcessoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task ObterPessoa_ProfessorForaDaTurma_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf11a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoForaId}");

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task ObterPessoa_ProfessorNaTurma_RetornaDtoReduzido()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf11b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            Assert.Equal(cenario.AlunoDentroId, raiz.GetProperty("id").GetInt32());
            Assert.Equal("Aluno Dentro rf11b", raiz.GetProperty("nome").GetString());
            Assert.Equal("Ativo", raiz.GetProperty("situacao").GetString());
            Assert.Equal("69999990001", raiz.GetProperty("celular").GetString());

            var parentescos = raiz.GetProperty("parentescos");
            Assert.Equal(1, parentescos.GetArrayLength());
            Assert.Equal("Responsavel rf11b", parentescos[0].GetProperty("parenteNome").GetString());
            Assert.Equal("Mãe", parentescos[0].GetProperty("tipoRelacionamento").GetString());
            Assert.Equal("69999990003", parentescos[0].GetProperty("parenteCelular").GetString());

            Assert.False(raiz.TryGetProperty("email", out _));
            Assert.False(raiz.TryGetProperty("endereco", out _));
            Assert.False(raiz.TryGetProperty("dataNascimento", out _));
            Assert.False(raiz.TryGetProperty("criadoEm", out _));
        }

        [Fact]
        public async Task ObterPessoa_UsuarioOutraPessoa_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf11c");
            var usuario = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailUsuarioComum);

            var outra = await usuario.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}");
            var propria = await usuario.GetAsync($"/api/pessoas/{cenario.UsuarioComumPessoaId}");

            Assert.Equal(HttpStatusCode.Forbidden, outra.StatusCode);
            Assert.Equal(HttpStatusCode.OK, propria.StatusCode);
        }

        [Fact]
        public async Task ObterPessoa_Admin_RetornaDtoCompletoDeQualquerPessoa()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf11d");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.GetAsync($"/api/pessoas/{cenario.AlunoForaId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.TryGetProperty("email", out _));
            Assert.True(json.RootElement.TryGetProperty("criadoEm", out _));
        }

        [Fact]
        public async Task ListarParentescos_ProfessorForaDaTurma_Retorna403()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf11e");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);

            var fora = await professor.GetAsync($"/api/pessoas/{cenario.AlunoForaId}/parentescos");
            var dentro = await professor.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}/parentescos");

            Assert.Equal(HttpStatusCode.Forbidden, fora.StatusCode);
            Assert.Equal(HttpStatusCode.OK, dentro.StatusCode);
        }
    }
}
