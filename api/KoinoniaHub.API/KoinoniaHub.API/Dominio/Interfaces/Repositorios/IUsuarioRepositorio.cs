using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IUsuarioRepositorio
    {
        Task<Usuario> CriarAsync(Usuario usuario);
        Task<Usuario?> ObterPorIdAsync(int igrejaId, int usuarioId);
        Task<Usuario?> ObterPorEmailAsync(string emailNormalizado);
        Task<List<Usuario>> ListarAsync(int igrejaId);
        Task AtualizarAsync(Usuario usuario);
    }
}