using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // RF21: a função da atribuição é lista fechada (Professor ou Auxiliar). Valor fora
    // dela volta como 400 ValidationProblemDetails em errors.Funcao, em criar e em editar,
    // sem gravar nada; os dois valores da lista continuam aceitos.
    public class RF21_FuncaoAtribuicaoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF21_FuncaoAtribuicaoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        [Fact]
        public async Task Criar_FuncaoForaDaLista_Retorna400ENadaGravado()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf21fa");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync("/api/atribuicoes", new
            {
                PessoaId = cenario.UsuarioComumPessoaId,
                DepartamentoId = cenario.TurmaSemAtribuicaoId,
                Funcao = "Coordenador",
                Ativo = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var erroFuncao = json.RootElement.GetProperty("errors").GetProperty("Funcao")[0].GetString();
            Assert.Equal("Função inválida: use Professor ou Auxiliar.", erroFuncao);

            Assert.Equal(0, await ContarAtribuicoesAsync(cenario.UsuarioComumPessoaId));
        }

        [Fact]
        public async Task Atualizar_FuncaoForaDaLista_Retorna400EFuncaoNaoMuda()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf21fb");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);
            var atribuicaoId = await CriarAuxiliarAsync(admin, cenario);

            var resposta = await admin.PutAsJsonAsync($"/api/atribuicoes/{atribuicaoId}", new { Funcao = "Lider", Ativo = true });

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("Funcao", out _));

            Assert.Equal("Auxiliar", await FuncaoGravadaAsync(atribuicaoId));
        }

        [Theory]
        [InlineData("Professor")]
        [InlineData("Auxiliar")]
        public async Task Criar_FuncaoDaLista_Retorna200EGrava(string funcao)
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, $"rf21fc{funcao.ToLower()}");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.PostAsJsonAsync("/api/atribuicoes", new
            {
                PessoaId = cenario.UsuarioComumPessoaId,
                DepartamentoId = cenario.TurmaSemAtribuicaoId,
                Funcao = funcao,
                Ativo = true
            });

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            Assert.Equal(funcao, json.RootElement.GetProperty("funcao").GetString());
            Assert.Equal(funcao, await FuncaoGravadaAsync(json.RootElement.GetProperty("id").GetInt32()));
        }

        private static async Task<int> CriarAuxiliarAsync(HttpClient admin, CenarioAcessoPessoa cenario)
        {
            var criada = await admin.PostAsJsonAsync("/api/atribuicoes", new
            {
                PessoaId = cenario.UsuarioComumPessoaId,
                DepartamentoId = cenario.TurmaSemAtribuicaoId,
                Funcao = "Auxiliar",
                Ativo = true
            });
            Assert.Equal(HttpStatusCode.OK, criada.StatusCode);

            using var json = JsonDocument.Parse(await criada.Content.ReadAsStringAsync());
            return json.RootElement.GetProperty("id").GetInt32();
        }

        private async Task<int> ContarAtribuicoesAsync(int pessoaId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Atribuicoes.AsNoTracking().CountAsync(a => a.PessoaId == pessoaId);
        }

        private async Task<string?> FuncaoGravadaAsync(int atribuicaoId)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            return await db.Atribuicoes.AsNoTracking()
                .Where(a => a.Id == atribuicaoId)
                .Select(a => a.Funcao)
                .FirstOrDefaultAsync();
        }
    }
}
