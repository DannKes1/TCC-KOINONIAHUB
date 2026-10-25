using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // RF30 — cadastro de aula: campos obrigatórios validados com mensagem clara (RNF 30.2).
    // Correção do SonarCloud (S6964): sem a data a aula era gravada com 0001-01-01, e sem
    // a matéria a requisição caía no 404 da guarda de autorização (matéria 0 inexistente).
    // Agora as duas voltam 400 ValidationProblemDetails do [ApiController], com o campo
    // em errors, e nada é gravado.
    public class RF30_CadastrarAulaTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF30_CadastrarAulaTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Criar_SemData_Retorna400ENaoGrava()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf30a");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var aulasAntes = await ContarAulasDaTurmaAsync(cenario.TurmaId);

            var resposta = await professor.PostAsJsonAsync("/api/aulas", new
            {
                Tema = "Aula sem data",
                MateriaId = cenario.MateriaId,
                ProfessorId = cenario.ProfessorPessoaId
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erros = await LerErrosAsync(resposta);
            Assert.Equal("Informe a data da aula.", Assert.Single(erros["Data"]));
            Assert.Equal(aulasAntes, await ContarAulasDaTurmaAsync(cenario.TurmaId));
        }

        [Fact]
        public async Task Criar_SemMateria_Retorna400ComCampo()
        {
            var cenario = await CenarioAula.CriarAsync(_fabrica, "rf30b");
            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var aulasAntes = await ContarAulasDaTurmaAsync(cenario.TurmaId);

            var resposta = await professor.PostAsJsonAsync("/api/aulas", new
            {
                Data = DateTime.UtcNow.Date,
                Tema = "Aula sem materia",
                ProfessorId = cenario.ProfessorPessoaId
            });

            // Antes da correção: 404 "Matéria não encontrada para esta igreja." (guarda).
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            var erros = await LerErrosAsync(resposta);
            Assert.Equal("Informe a matéria.", Assert.Single(erros["MateriaId"]));
            Assert.Equal(aulasAntes, await ContarAulasDaTurmaAsync(cenario.TurmaId));
        }

        private async Task<int> ContarAulasDaTurmaAsync(int turmaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Aulas.CountAsync(a => a.Materia.DepartamentoId == turmaId);
        }

        // errors do ValidationProblemDetails: campo → mensagens.
        private static async Task<Dictionary<string, List<string?>>> LerErrosAsync(HttpResponseMessage resposta)
        {
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("errors").EnumerateObject()
                .ToDictionary(c => c.Name, c => c.Value.EnumerateArray().Select(m => m.GetString()).ToList());
        }
    }
}
