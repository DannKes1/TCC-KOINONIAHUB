using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IPresencaRepositorio
    {
        Task<List<Presenca>> ListarPorAulaAsync(int igrejaId, int aulaId);
        Task<Presenca?> ObterPorChaveAsync(int aulaId, int alunoDepartamentoId);
        Task<Presenca> CriarAsync(Presenca presenca);
        Task AtualizarAsync(Presenca presenca);
    }
}