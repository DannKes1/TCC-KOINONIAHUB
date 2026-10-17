using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Infraestrutura.Dados;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KoinoniaHub.API.Tests
{
    // RF12 — parentesco derivado na leitura (RNFs 12.3/12.4; CSU13 passo 2 e FA1; Plano 6.15).
    // O vínculo é gravado uma vez no cadastro de origem; no cadastro do parente aparece o grau
    // inverso com derivado: true, e a remoção por esse lado é recusada.
    public class RF12_ParentescoDerivadoTests : IClassFixture<KoinoniaHubWebApplicationFactory>
    {
        private readonly KoinoniaHubWebApplicationFactory _fabrica;

        public RF12_ParentescoDerivadoTests(KoinoniaHubWebApplicationFactory fabrica)
        {
            _fabrica = fabrica;
        }

        // Cria, na igreja do cenário, a pessoa de origem (com o sexo informado) e o parente, e grava
        // um único Parentesco origem → parente com o tipo dado. Devolve (origemId, parenteId, parentescoId).
        private async Task<(int OrigemId, int ParenteId, int ParentescoId)> RegistrarAsync(
            CenarioAcessoPessoa cenario, string sufixo, string tipo, string? sexoOrigem)
        {
            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();

            var origem = new Pessoa { Nome = $"Origem {sufixo}", Sexo = sexoOrigem, Celular = "69911110001", IgrejaId = cenario.IgrejaId };
            var parente = new Pessoa { Nome = $"Parente {sufixo}", IgrejaId = cenario.IgrejaId };
            var vinculo = new Parentesco { Pessoa = origem, Parente = parente, TipoRelacionamento = tipo };
            db.Add(vinculo);
            await db.SaveChangesAsync();

            return (origem.Id, parente.Id, vinculo.Id);
        }

        private static async Task<JsonElement[]> ListarAsync(HttpClient cliente, int pessoaId)
        {
            var resposta = await cliente.GetAsync($"/api/pessoas/{pessoaId}/parentescos");
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            return json.RootElement.EnumerateArray().Select(e => e.Clone()).ToArray();
        }

        [Theory]
        [InlineData("Pai")]
        [InlineData("Mãe")]
        public async Task PaiOuMae_NoCadastroDoParente_ApareceFilhoDerivado(string tipo)
        {
            var sufixo = $"rf12a{tipo.Length}{(int)tipo[0]}";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, sufixo);
            var (origemId, parenteId, parentescoId) = await RegistrarAsync(cenario, sufixo, tipo, null);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // Lado de origem: só o registro explícito.
            var origem = await ListarAsync(admin, origemId);
            Assert.Single(origem);
            Assert.Equal(tipo, origem[0].GetProperty("tipoRelacionamento").GetString());
            Assert.False(origem[0].GetProperty("derivado").GetBoolean());

            // Lado do parente: a derivada, com o Id do registro de origem e sem segundo registro.
            var parente = await ListarAsync(admin, parenteId);
            Assert.Single(parente);
            Assert.Equal("Filho(a)", parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parente[0].GetProperty("derivado").GetBoolean());
            Assert.Equal(parentescoId, parente[0].GetProperty("id").GetInt32());
            Assert.Equal(parenteId, parente[0].GetProperty("pessoaId").GetInt32());
            Assert.Equal(origemId, parente[0].GetProperty("parenteId").GetInt32());
            Assert.Equal($"Origem {sufixo}", parente[0].GetProperty("parenteNome").GetString());
            Assert.Equal("69911110001", parente[0].GetProperty("parenteCelular").GetString());

            using var escopo = _fabrica.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
            Assert.Equal(1, await db.Parentescos.CountAsync(p => p.PessoaId == origemId || p.ParenteId == origemId));
        }

        [Theory]
        [InlineData("Masculino", "Pai")]
        [InlineData("Feminino", "Mãe")]
        [InlineData(null, "Pai ou mãe")]
        public async Task Filho_DerivaPeloSexoDaPessoaDeOrigem(string? sexoOrigem, string esperado)
        {
            var sufixo = $"rf12b{sexoOrigem?.Length ?? 0}";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, sufixo);
            var (_, parenteId, _) = await RegistrarAsync(cenario, sufixo, "Filho(a)", sexoOrigem);
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var parente = await ListarAsync(admin, parenteId);

            Assert.Single(parente);
            Assert.Equal(esperado, parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parente[0].GetProperty("derivado").GetBoolean());
        }

        [Theory]
        [InlineData("Irmão(ã)")]
        [InlineData("Cônjuge")]
        public async Task Simetricos_DerivamOMesmoTipo(string tipo)
        {
            var sufixo = $"rf12c{(int)tipo[0]}";
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, sufixo);
            var (_, parenteId, _) = await RegistrarAsync(cenario, sufixo, tipo, "Masculino");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var parente = await ListarAsync(admin, parenteId);

            Assert.Single(parente);
            Assert.Equal(tipo, parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parente[0].GetProperty("derivado").GetBoolean());
        }

        [Fact]
        public async Task Responsavel_DerivaDependente()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12d");
            var (_, parenteId, _) = await RegistrarAsync(cenario, "rf12d", "Responsável", "Feminino");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var parente = await ListarAsync(admin, parenteId);

            Assert.Single(parente);
            Assert.Equal("Dependente", parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parente[0].GetProperty("derivado").GetBoolean());
        }

        [Fact]
        public async Task TextoLivre_MantemOTextoMarcandoAOrigem()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12e");
            var (_, parenteId, _) = await RegistrarAsync(cenario, "rf12e", "Padrinho", "Masculino");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var parente = await ListarAsync(admin, parenteId);

            Assert.Single(parente);
            Assert.Equal("Padrinho (informado no cadastro de Origem rf12e)", parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parente[0].GetProperty("derivado").GetBoolean());
        }

        [Fact]
        public async Task RegistroExplicitoInverso_OmiteADerivada()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12f");
            var (origemId, parenteId, _) = await RegistrarAsync(cenario, "rf12f", "Pai", "Masculino");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            // O parente registra, no próprio cadastro, o vínculo com a origem (qualquer tipo).
            var criar = await admin.PostAsJsonAsync($"/api/pessoas/{parenteId}/parentescos",
                new { ParenteId = origemId, TipoRelacionamento = "Filho(a)" });
            Assert.Equal(HttpStatusCode.OK, criar.StatusCode);

            var parente = await ListarAsync(admin, parenteId);

            Assert.Single(parente);
            Assert.Equal("Filho(a)", parente[0].GetProperty("tipoRelacionamento").GetString());
            Assert.False(parente[0].GetProperty("derivado").GetBoolean());

            // No cadastro de origem o explícito "Pai" continua, e o novo registro do parente
            // também não gera derivada lá, porque a origem já tem registro explícito com o parente.
            var origem = await ListarAsync(admin, origemId);
            Assert.Single(origem);
            Assert.Equal("Pai", origem[0].GetProperty("tipoRelacionamento").GetString());
            Assert.False(origem[0].GetProperty("derivado").GetBoolean());
        }

        [Fact]
        public async Task Remover_PeloLadoDerivado_Retorna400EPreservaORegistro()
        {
            // RNF 12.3 (segunda frase); CSU13 FA1.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12g");
            var (origemId, parenteId, parentescoId) = await RegistrarAsync(cenario, "rf12g", "Mãe", "Feminino");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.DeleteAsync($"/api/pessoas/{parenteId}/parentescos/{parentescoId}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            using (var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()))
                Assert.Equal("Este vínculo foi registrado no cadastro de Origem rf12g e só pode ser removido por lá.",
                    json.RootElement.GetProperty("mensagem").GetString());

            using (var escopo = _fabrica.Services.CreateScope())
            {
                var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
                Assert.True(await db.Parentescos.AnyAsync(p => p.Id == parentescoId));
            }

            // Pelo cadastro de origem a remoção funciona e a derivada some do parente.
            var remover = await admin.DeleteAsync($"/api/pessoas/{origemId}/parentescos/{parentescoId}");
            Assert.Equal(HttpStatusCode.NoContent, remover.StatusCode);
            Assert.Empty(await ListarAsync(admin, parenteId));
        }

        [Fact]
        public async Task Remover_IdInexistente_ContinuaRetornando404()
        {
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12h");
            var admin = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailAdmin);

            var resposta = await admin.DeleteAsync($"/api/pessoas/{cenario.AlunoDentroId}/parentescos/999999");

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task ObterPessoaParaTurma_ProfessorVeADerivada()
        {
            // RNF 11.3: o DTO reduzido do Professor também traz o grau inverso.
            var cenario = await CenarioAcessoPessoa.CriarAsync(_fabrica, "rf12i");

            using (var escopo = _fabrica.Services.CreateScope())
            {
                var db = escopo.ServiceProvider.GetRequiredService<KoinoniaHubDbContext>();
                // Aluno Fora registra o Aluno Dentro como Irmão(ã); no cadastro do Aluno Dentro
                // aparece a derivada, além da "Mãe" explícita do cenário.
                db.Add(new Parentesco { PessoaId = cenario.AlunoForaId, ParenteId = cenario.AlunoDentroId, TipoRelacionamento = "Irmão(ã)" });
                await db.SaveChangesAsync();
            }

            var professor = await CenarioAcessoPessoa.ClienteAutenticadoAsync(_fabrica, cenario.EmailProfessor);
            var resposta = await professor.GetAsync($"/api/pessoas/{cenario.AlunoDentroId}");

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var parentescos = json.RootElement.GetProperty("parentescos").EnumerateArray().ToArray();

            Assert.Equal(2, parentescos.Length);
            Assert.Equal("Mãe", parentescos[0].GetProperty("tipoRelacionamento").GetString());
            Assert.False(parentescos[0].GetProperty("derivado").GetBoolean());
            Assert.Equal("Aluno Fora rf12i", parentescos[1].GetProperty("parenteNome").GetString());
            Assert.Equal("Irmão(ã)", parentescos[1].GetProperty("tipoRelacionamento").GetString());
            Assert.True(parentescos[1].GetProperty("derivado").GetBoolean());
            Assert.Equal("69999990002", parentescos[1].GetProperty("parenteCelular").GetString());
        }
    }
}
