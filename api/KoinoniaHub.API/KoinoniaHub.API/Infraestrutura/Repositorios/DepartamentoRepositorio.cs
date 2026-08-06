using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class DepartamentoRepositorio : IDepartamentoRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public DepartamentoRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Departamento> CriarAsync(Departamento departamento)
        {
            _db.Departamentos.Add(departamento);
            await _db.SaveChangesAsync();
            return departamento;
        }

        public async Task<List<Departamento>> ListarAsync(int igrejaId)
        {
            return await _db.Departamentos
                .AsNoTracking()
                .Where(d => d.IgrejaId == igrejaId)
                .OrderBy(d => d.Nome)
                .ToListAsync();
        }

        public async Task<Departamento?> ObterPorIdAsync(int igrejaId, int departamentoId)
        {
            return await _db.Departamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IgrejaId == igrejaId && d.Id == departamentoId);
        }

        public async Task AtualizarAsync(Departamento departamento)
        {
            _db.Departamentos.Update(departamento);
            await _db.SaveChangesAsync();
        }
    }
}