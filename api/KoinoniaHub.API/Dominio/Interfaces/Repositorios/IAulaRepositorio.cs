using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IAulaRepositorio
    {
        Task<Aula> CriarAsync(Aula aula);
        Task<Aula?> ObterPorIdAsync(int igrejaId, int aulaId);
        Task<List<Aula>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
    }
}