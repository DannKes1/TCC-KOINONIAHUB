using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Aplicacao.Regras
{
    // Corretiva G2 (RNFs 7.x, 8.x e 13.5): um e-mail por pessoa, compartilhado entre o
    // cadastro (Pessoa.Email) e a conta de acesso (Usuario.Email). Dentro da igreja, um
    // e-mail não pode estar no cadastro de uma pessoa e na conta de outra. A regra global
    // de Usuario.Email único entre igrejas (índice único) continua separada.
    public static class EmailDaPessoa
    {
        public const string MensagemEmUso = "Este e-mail já está em uso por outra pessoa.";
        public const string MensagemDaConta = "O e-mail de uma pessoa com conta de acesso não pode ser alterado.";

        public static string? Normalizar(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        // Verdadeiro quando outra pessoa da igreja já usa o e-mail, no cadastro ou na conta.
        // `pessoaId` e `usuarioId` identificam a própria pessoa e a própria conta (0 quando
        // ainda não existem), que ficam fora da comparação.
        public static async Task<bool> EmUsoPorOutraPessoaAsync(
            KoinoniaHubDbContext db, int igrejaId, int pessoaId, int usuarioId, string emailNormalizado)
        {
            var outraPessoa = await db.Pessoas.AnyAsync(p =>
                p.IgrejaId == igrejaId && p.Id != pessoaId && p.Email != null && p.Email.ToLower() == emailNormalizado);
            if (outraPessoa) return true;

            return await db.Usuarios.AnyAsync(u =>
                u.IgrejaId == igrejaId && u.Id != usuarioId && u.PessoaId != pessoaId && u.Email.ToLower() == emailNormalizado);
        }

        public static async Task GarantirDisponivelAsync(
            KoinoniaHubDbContext db, int igrejaId, int pessoaId, int usuarioId, string emailNormalizado)
        {
            if (await EmUsoPorOutraPessoaAsync(db, igrejaId, pessoaId, usuarioId, emailNormalizado))
                throw new InvalidOperationException(MensagemEmUso);
        }
    }
}
