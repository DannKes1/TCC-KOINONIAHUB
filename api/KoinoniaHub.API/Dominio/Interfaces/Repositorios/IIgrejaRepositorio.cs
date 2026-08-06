using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IIgrejaRepositorio
    {
        Task<Igreja?> ObterPorIdAsync(int id);
        Task<Igreja> CriarAsync(Igreja igreja);
    }
}
