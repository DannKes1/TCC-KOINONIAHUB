using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class PresencaRepositorio : IPresencaRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public PresencaRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<List<Presenca>> ListarPorAulaAsync(int igrejaId, int aulaId)
        {
            return await _db.Presencas
                .AsNoTracking()
                .Include(p => p.AlunoDepartamento)
                    .ThenInclude(m => m.Pessoa)
                .Include(p => p.Aula)
                    .ThenInclude(a => a.Materia)
                        .ThenInclude(m => m.Departamento)
                .Where(p =>
                    p.AulaId == aulaId &&
                    p.Aula.Materia.Departamento.IgrejaId == igrejaId &&
                    p.AlunoDepartamento.Pessoa.IgrejaId == igrejaId)
                .OrderBy(p => p.AlunoDepartamento.Pessoa.Nome)
                .ToListAsync();
        }

        public async Task<Presenca?> ObterPorChaveAsync(int aulaId, int alunoDepartamentoId)
        {
            return await _db.Presencas
                .FirstOrDefaultAsync(p => p.AulaId == aulaId && p.AlunoDepartamentoId == alunoDepartamentoId);
        }

        public async Task<Presenca> CriarAsync(Presenca presenca)
        {
            _db.Presencas.Add(presenca);
            await _db.SaveChangesAsync();
            return presenca;
        }

        public async Task AtualizarAsync(Presenca presenca)
        {
            _db.Presencas.Update(presenca);
            await _db.SaveChangesAsync();
        }
    }
}