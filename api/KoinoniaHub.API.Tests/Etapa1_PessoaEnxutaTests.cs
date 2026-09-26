using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests
{
    public class Etapa1_PessoaEnxutaTests
    {
        private static readonly string[] ColunasDoDer =
        {
            "Id", "Nome", "Situacao", "Sexo", "DataNascimento", "EstadoCivil", "Email", "Celular",
            "Endereco", "Bairro", "Cidade", "Estado", "CEP", "DataInativacao", "CriadoEm", "AtualizadoEm", "IgrejaId"
        };

        [Fact]
        public void Pessoa_ModeloMapeado_TemExatamenteAsColunasDoDer()
        {
            using var ctx = new ContextoDeTeste();

            var tipo = ctx.Db.Model.FindEntityType(typeof(Pessoa));
            Assert.NotNull(tipo);

            var colunas = tipo!.GetProperties().Select(p => p.Name).OrderBy(n => n).ToArray();

            Assert.Equal(ColunasDoDer.OrderBy(n => n).ToArray(), colunas);
        }

        [Fact]
        public async Task Pessoa_ApenasComNome_Persiste()
        {
            using var ctx = new ContextoDeTeste();

            var igreja = new Igreja { Nome = "Igreja de Teste" };
            var pessoa = new Pessoa { Nome = "Somente Nome", Igreja = igreja };

            ctx.Db.Pessoas.Add(pessoa);
            await ctx.Db.SaveChangesAsync();

            using var leitura = ctx.NovoContexto();
            var lida = await leitura.Pessoas.SingleAsync(p => p.Id == pessoa.Id);

            Assert.Equal("Somente Nome", lida.Nome);
            Assert.Equal("Ativo", lida.Situacao);
            Assert.Null(lida.DataInativacao);
        }
    }
}
