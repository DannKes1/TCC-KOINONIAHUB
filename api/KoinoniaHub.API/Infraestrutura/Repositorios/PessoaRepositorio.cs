using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class PessoaRepositorio : IPessoaRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public PessoaRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Pessoa> CriarAsync(Pessoa pessoa)
        {
            _db.Pessoas.Add(pessoa);
            await _db.SaveChangesAsync();
            return pessoa;
        }

        public async Task<Pessoa?> ObterPorIdAsync(int igrejaId, int pessoaId)
        {
            return await _db.Pessoas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IgrejaId == igrejaId && p.Id == pessoaId);
        }

        public async Task<List<Pessoa>> ListarAsync(int igrejaId)
        {
            return await _db.Pessoas
                .AsNoTracking()
                .Where(p => p.IgrejaId == igrejaId)
                .OrderBy(p => p.Nome)
                .ToListAsync();
        }

        public async Task AtualizarAsync(Pessoa pessoa)
        {
            _db.Pessoas.Update(pessoa);
            await _db.SaveChangesAsync();
        }
    }
}
