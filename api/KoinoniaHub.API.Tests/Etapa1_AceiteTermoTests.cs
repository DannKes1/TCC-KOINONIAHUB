using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Tests.Infraestrutura;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Tests
{
    public class Etapa1_AceiteTermoTests
    {
        [Fact]
        public async Task AceiteTermo_Gravado_PersisteTodosOsCampos()
        {
            using var ctx = new ContextoDeTeste();

            var usuario = NovoUsuarioMinimo();
            var aceitoEm = new DateTime(2026, 9, 25, 12, 30, 0, DateTimeKind.Utc);

            var aceite = new AceiteTermo
            {
                Usuario = usuario,
                Igreja = usuario.Igreja,
                TermoVersao = "1.0",
                TermoHash = new string('a', 64),
                AceitoEm = aceitoEm,
                Ip = "192.168.0.10",
                Meio = MeioAceiteTermo.CadastroInicial
            };

            ctx.Db.AceitesTermo.Add(aceite);
            await ctx.Db.SaveChangesAsync();

            using var leitura = ctx.NovoContexto();
            var lido = await leitura.AceitesTermo.SingleAsync(a => a.Id == aceite.Id);

            Assert.Equal(usuario.Id, lido.UsuarioId);
            Assert.Equal(usuario.IgrejaId, lido.IgrejaId);
            Assert.Equal("1.0", lido.TermoVersao);
            Assert.Equal(new string('a', 64), lido.TermoHash);
            Assert.Equal(aceitoEm, lido.AceitoEm);
            Assert.Equal("192.168.0.10", lido.Ip);
            Assert.Equal(MeioAceiteTermo.CadastroInicial, lido.Meio);
            Assert.NotEqual(default, lido.CriadoEm);
        }

        [Fact]
        public async Task Usuario_ComAceiteRegistrado_NaoPodeSerExcluido()
        {
            using var ctx = new ContextoDeTeste();

            var usuario = NovoUsuarioMinimo();
            ctx.Db.AceitesTermo.Add(new AceiteTermo
            {
                Usuario = usuario,
                Igreja = usuario.Igreja,
                TermoVersao = "1.0",
                TermoHash = new string('b', 64),
                AceitoEm = DateTime.UtcNow,
                Meio = MeioAceiteTermo.Login
            });
            await ctx.Db.SaveChangesAsync();

            using var outro = ctx.NovoContexto();
            var alvo = await outro.Usuarios.SingleAsync(u => u.Id == usuario.Id);
            outro.Usuarios.Remove(alvo);

            await Assert.ThrowsAsync<DbUpdateException>(() => outro.SaveChangesAsync());
        }

        [Fact]
        public void MeioAceiteTermo_ExpoeOsTresMeiosDaRnf42_5()
        {
            Assert.Equal(
                new[] { "CadastroInicial", "PrimeiroAcesso", "Login" },
                MeioAceiteTermo.Todos);
        }

        private static Usuario NovoUsuarioMinimo()
        {
            var igreja = new Igreja { Nome = "Igreja de Teste" };

            return new Usuario
            {
                Email = "usuario.aceite@teste.com",
                SenhaHash = "hash-de-teste",
                Perfil = "Usuario",
                Igreja = igreja
            };
        }
    }
}
