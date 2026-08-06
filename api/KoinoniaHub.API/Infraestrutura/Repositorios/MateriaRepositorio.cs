using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class MateriaRepositorio : IMateriaRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public MateriaRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Materia> CriarAsync(Materia materia)
        {
            _db.Materias.Add(materia);
            await _db.SaveChangesAsync();
            return materia;
        }

        public async Task<List<Materia>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId)
        {
            return await _db.Materias
                .AsNoTracking()
                .Include(m => m.Departamento)
                .Where(m => m.DepartamentoId == departamentoId && m.Departamento.IgrejaId == igrejaId)
                .OrderBy(m => m.OrdemExibicao)
                .ThenBy(m => m.Nome)
                .ToListAsync();
        }

        public async Task<Materia?> ObterPorIdAsync(int igrejaId, int materiaId)
        {
            return await _db.Materias
                .Include(m => m.Departamento)
                .FirstOrDefaultAsync(m => m.Id == materiaId && m.Departamento.IgrejaId == igrejaId);
        }

        public async Task AtualizarAsync(Materia materia)
        {
            _db.Materias.Update(materia);
            await _db.SaveChangesAsync();
        }
    }
}