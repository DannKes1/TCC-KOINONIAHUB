using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class IgrejaRepositorio : IIgrejaRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public IgrejaRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Igreja?> ObterPorIdAsync(int id)
        {
            return await _db.Igrejas.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Igreja> CriarAsync(Igreja igreja)
        {
            _db.Igrejas.Add(igreja);
            await _db.SaveChangesAsync();
            return igreja;
        }
    }
}
