using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class AulaRepositorio : IAulaRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public AulaRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Aula> CriarAsync(Aula aula)
        {
            _db.Aulas.Add(aula);
            await _db.SaveChangesAsync();
            return aula;
        }

        public async Task<Aula?> ObterPorIdAsync(int igrejaId, int aulaId)
        {
            return await _db.Aulas
                .Include(a => a.Materia)
                    .ThenInclude(m => m.Departamento)
                .Include(a => a.Professor)
                .FirstOrDefaultAsync(a =>
                    a.Id == aulaId &&
                    a.Materia.Departamento.IgrejaId == igrejaId &&
                    a.Professor.IgrejaId == igrejaId);
        }

        public async Task<List<Aula>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId)
        {
            return await _db.Aulas
                .AsNoTracking()
                .Include(a => a.Materia)
                    .ThenInclude(m => m.Departamento)
                .Include(a => a.Professor)
                .Where(a =>
                    a.Materia.DepartamentoId == departamentoId &&
                    a.Materia.Departamento.IgrejaId == igrejaId)
                .OrderByDescending(a => a.Data)
                .ToListAsync();
        }
    }
}