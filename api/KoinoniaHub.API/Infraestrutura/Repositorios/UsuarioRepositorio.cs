using KoinoniaHub.API.Dominio.Entidades;
using KoinoniaHub.API.Dominio.Interfaces.Repositorios;
using KoinoniaHub.API.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;

namespace KoinoniaHub.API.Infraestrutura.Repositorios
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly KoinoniaHubDbContext _db;

        public UsuarioRepositorio(KoinoniaHubDbContext db)
        {
            _db = db;
        }

        public async Task<Usuario> CriarAsync(Usuario usuario)
        {
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return usuario;
        }

        public async Task<Usuario?> ObterPorIdAsync(int igrejaId, int usuarioId)
        {
            return await _db.Usuarios
                .Include(u => u.Pessoa)
                .FirstOrDefaultAsync(u => u.IgrejaId == igrejaId && u.Id == usuarioId);
        }

        public async Task<Usuario?> ObterPorEmailAsync(string emailNormalizado)
        {
            return await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalizado);
        }

        public async Task<List<Usuario>> ListarAsync(int igrejaId)
        {
            return await _db.Usuarios
                .AsNoTracking()
                .Include(u => u.Pessoa)
                .Where(u => u.IgrejaId == igrejaId)
                .OrderBy(u => u.Email)
                .ToListAsync();
        }

        public async Task AtualizarAsync(Usuario usuario)
        {
            _db.Usuarios.Update(usuario);
            await _db.SaveChangesAsync();
        }
    }
}