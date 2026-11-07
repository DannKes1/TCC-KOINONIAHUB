using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // RF22: reativar uma atribuição encerrada quando a pessoa já tem outra ativa com a mesma
    // função na turma é recusado com a mensagem da criação; sem a outra ativa, reativa.
    public class RF22_ReativarAtribuicaoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private const string MensagemDuplicada = "Esta pessoa já possui uma atribuição ativa com esta função neste departamento.";

        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF22_ReativarAtribuicaoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task ReativarEncerrada_ComOutraAtivaIgual_Retorna400ENaoDuplica()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf22g6");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var antigaId = await LerAsync(db => db.Atribuicoes
                .Where(a => a.PessoaId == cenario.ProfessorPessoaId).Select(a => a.Id).SingleAsync());

            Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsync($"/api/atribuicoes/{antigaId}/encerrar", content: null)).StatusCode);
            var nova = await admin.PostAsJsonAsync("/api/atribuicoes", new
            {
                PessoaId = cenario.ProfessorPessoaId,
                DepartamentoId = cenario.TurmaComAtribuicaoId,
                Funcao = "Professor",
                Ativo = true
            });
            Assert.True(nova.IsSuccessStatusCode);

            var resposta = await admin.PutAsJsonAsync($"/api/atribuicoes/{antigaId}", new { Funcao = "Professor", Ativo = true });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
                Assert.Equal(MensagemDuplicada, json.RootElement.GetProperty("mensagem").GetString());
            Assert.Equal(1, await AtivasDoProfessorAsync(cenario.ProfessorPessoaId));

            // Encerrada a nova, a antiga pode voltar.
            var novaId = await LerAsync(db => db.Atribuicoes
                .Where(a => a.PessoaId == cenario.ProfessorPessoaId && a.Ativo).Select(a => a.Id).SingleAsync());
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsync($"/api/atribuicoes/{novaId}/encerrar", content: null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await admin.PutAsJsonAsync($"/api/atribuicoes/{antigaId}", new { Funcao = "Professor", Ativo = true })).StatusCode);
            Assert.True(await LerAsync(db => db.Atribuicoes.Where(a => a.Id == antigaId).Select(a => a.Ativo).SingleAsync()));
            Assert.Equal(1, await AtivasDoProfessorAsync(cenario.ProfessorPessoaId));
        }

        private Task<int> AtivasDoProfessorAsync(int pessoaId) =>
            LerAsync(db => db.Atribuicoes.CountAsync(a => a.PessoaId == pessoaId && a.Ativo));

        private async Task<T> LerAsync<T>(Func<KoinoniaHubDbContext, Task<T>> consulta)
        {
            using var escopo = _fabrica.Services.CreateScope();
            return await consulta(escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>());
        }
    }
}
