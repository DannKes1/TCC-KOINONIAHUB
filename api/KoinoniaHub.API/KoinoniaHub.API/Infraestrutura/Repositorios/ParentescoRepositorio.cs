using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class ParentescoRepositorio : IParentescoRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public ParentescoRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Parentesco> CriarAsync(Parentesco parentesco)
        {
            _db.Parentescos.Add(parentesco);
            await _db.SaveChangesAsync();
            return parentesco;
        }

        public async Task<List<Parentesco>> ListarDaPessoaAsync(int igrejaId, int pessoaId)
        {
            return await _db.Parentescos
                .AsNoTracking()
                .Include(p => p.Parente)
                .Include(p => p.Pessoa)
                .Where(p => p.PessoaId == pessoaId && p.Pessoa.IgrejaId == igrejaId)
                .OrderBy(p => p.TipoRelacionamento)
                .ThenBy(p => p.Parente.Nome)
                .ToListAsync();
        }

        public async Task<Parentesco?> ObterPorIdAsync(int igrejaId, int pessoaId, int parentescoId)
        {
            return await _db.Parentescos
                .Include(p => p.Pessoa)
                .FirstOrDefaultAsync(p =>
                    p.Id == parentescoId &&
                    p.PessoaId == pessoaId &&
                    p.Pessoa.IgrejaId == igrejaId);
        }

        public async Task RemoverAsync(Parentesco parentesco)
        {
            _db.Parentescos.Remove(parentesco);
            await _db.SaveChangesAsync();
        }
    }
}