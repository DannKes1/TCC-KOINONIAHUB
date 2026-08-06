using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IMateriaRepositorio
    {
        Task<Materia> CriarAsync(Materia materia);
        Task<List<Materia>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
        Task<Materia?> ObterPorIdAsync(int igrejaId, int materiaId);
        Task AtualizarAsync(Materia materia);
    }
}