using System.Text;
using KoinoniaHub.API.Aplicacao.Servicos.Implementacoes;
using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests
{
    public class RF41_ImportacaoTests
    {
        [Fact]
        public async Task Importar_ColunasDoModelo_PreencheOsCampos()
        {
            using var ctx = new ContextoDeTeste();
            var igrejaId = await SemearIgrejaAsync(ctx);
            var servico = new PessoaImportacaoServico(ctx.Db);

            var csv =
                "Nome;Sexo;DataNascimento;EstadoCivil;Email;Celular;Endereco;Bairro;Cidade;Estado;CEP\n" +
                "Maria da Silva;Feminino;05/03/1990;Casada;maria@teste.com;69999990000;Rua A, 10;Centro;Ji-Paraná;RO;76900-000\n";

            var resposta = await servico.ImportarCsvAsync(igrejaId, Fluxo(csv));

            Assert.Equal(1, resposta.Criados);
            Assert.Equal(0, resposta.Ignorados);
            Assert.Equal(0, resposta.Erros);

            var pessoa = await LerUnicaPessoaAsync(ctx, igrejaId);
            Assert.Equal("Maria da Silva", pessoa.Nome);
            Assert.Equal("Feminino", pessoa.Sexo);
            Assert.Equal(new DateTime(1990, 3, 5, 0, 0, 0, DateTimeKind.Utc), pessoa.DataNascimento);
            Assert.Equal("Casada", pessoa.EstadoCivil);
            Assert.Equal("maria@teste.com", pessoa.Email);
            Assert.Equal("69999990000", pessoa.Celular);
            Assert.Equal("Rua A, 10", pessoa.Endereco);
            Assert.Equal("Centro", pessoa.Bairro);
            Assert.Equal("Ji-Paraná", pessoa.Cidade);
            Assert.Equal("RO", pessoa.Estado);
            Assert.Equal("76900-000", pessoa.CEP);
            Assert.Equal("Ativo", pessoa.Situacao);
        }

        [Fact]
        public async Task Importar_ColunaForaDoModelo_Ignorada()
        {
            using var ctx = new ContextoDeTeste();
            var igrejaId = await SemearIgrejaAsync(ctx);
            var servico = new PessoaImportacaoServico(ctx.Db);

            var csv =
                "Nome;CPF;Categoria;Telefone;Situacao;Observacoes\n" +
                "João Pereira;123.456.789-00;Visitante;6932220000;Inativo;texto livre\n";

            var resposta = await servico.ImportarCsvAsync(igrejaId, Fluxo(csv));

            Assert.Equal(1, resposta.Criados);
            Assert.Equal("Criado", resposta.Itens.Single().Status);

            var pessoa = await LerUnicaPessoaAsync(ctx, igrejaId);
            Assert.Equal("João Pereira", pessoa.Nome);
            Assert.Equal("Ativo", pessoa.Situacao);
            Assert.Null(pessoa.DataInativacao);
        }

        [Fact]
        public async Task Importar_EmailRepetido_Ignora()
        {
            using var ctx = new ContextoDeTeste();
            var igrejaId = await SemearIgrejaAsync(ctx);
            ctx.Db.Pessoas.Add(new Pessoa { Nome = "Ana Existente", Email = "ana@teste.com", IgrejaId = igrejaId });
            await ctx.Db.SaveChangesAsync();
            var servico = new PessoaImportacaoServico(ctx.Db);

            var csv =
                "Nome;Email\n" +
                "Ana Outra Grafia;ANA@teste.com\n";

            var resposta = await servico.ImportarCsvAsync(igrejaId, Fluxo(csv));

            Assert.Equal(0, resposta.Criados);
            Assert.Equal(1, resposta.Ignorados);
            Assert.Equal("Ignorado", resposta.Itens.Single().Status);
            Assert.Equal(1, await ContarPessoasAsync(ctx, igrejaId));
        }

        [Fact]
        public async Task Importar_NomeRepetidoSemEmail_IgnoraESinaliza()
        {
            using var ctx = new ContextoDeTeste();
            var igrejaId = await SemearIgrejaAsync(ctx);
            ctx.Db.Pessoas.Add(new Pessoa { Nome = "Carlos Souza", IgrejaId = igrejaId });
            await ctx.Db.SaveChangesAsync();
            var servico = new PessoaImportacaoServico(ctx.Db);

            var csv =
                "Nome;Email\n" +
                "carlos souza;\n";

            var resposta = await servico.ImportarCsvAsync(igrejaId, Fluxo(csv));

            var item = resposta.Itens.Single();
            Assert.Equal(1, resposta.Ignorados);
            Assert.Equal("Ignorado", item.Status);
            Assert.Equal(
                "Já existe uma pessoa com este nome; linha ignorada para conferência. Se for outra pessoa, cadastre manualmente.",
                item.Mensagem);
            Assert.Equal(1, await ContarPessoasAsync(ctx, igrejaId));
        }

        [Fact]
        public async Task Importar_MaisDe1000Linhas_Retorna400()
        {
            using var ctx = new ContextoDeTeste();
            var igrejaId = await SemearIgrejaAsync(ctx);
            var servico = new PessoaImportacaoServico(ctx.Db);

            var sb = new StringBuilder("Nome\n");
            for (var i = 1; i <= 1001; i++)
                sb.Append("Pessoa ").Append(i).Append('\n');

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => servico.ImportarCsvAsync(igrejaId, Fluxo(sb.ToString())));

            Assert.Contains("1001 linhas", ex.Message);
            Assert.Equal(0, await ContarPessoasAsync(ctx, igrejaId));
        }

        private static async Task<int> SemearIgrejaAsync(ContextoDeTeste ctx)
        {
            var igreja = new Igreja { Nome = "Igreja de Teste" };
            ctx.Db.Igrejas.Add(igreja);
            await ctx.Db.SaveChangesAsync();
            return igreja.Id;
        }

        private static async Task<Pessoa> LerUnicaPessoaAsync(ContextoDeTeste ctx, int igrejaId)
        {
            using var leitura = ctx.NovoContexto();
            return await leitura.Pessoas.SingleAsync(p => p.IgrejaId == igrejaId);
        }

        private static async Task<int> ContarPessoasAsync(ContextoDeTeste ctx, int igrejaId)
        {
            using var leitura = ctx.NovoContexto();
            return await leitura.Pessoas.CountAsync(p => p.IgrejaId == igrejaId);
        }

        private static MemoryStream Fluxo(string csv) => new(Encoding.UTF8.GetBytes(csv));
    }
}
