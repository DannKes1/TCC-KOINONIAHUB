using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IDepartamentoRepositorio
    {
        Task<Departamento> CriarAsync(Departamento departamento);
        Task<List<Departamento>> ListarAsync(int igrejaId);
        Task<Departamento?> ObterPorIdAsync(int igrejaId, int departamentoId);
        Task AtualizarAsync(Departamento departamento);
    }
}