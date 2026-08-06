using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IAlunoDepartamentoRepositorio
    {
        Task<AlunoDepartamento?> ObterAtivaAsync(int igrejaId, int departamentoId, int pessoaId);
        Task<AlunoDepartamento> CriarAsync(AlunoDepartamento matricula);
        Task<List<AlunoDepartamento>> ListarAlunosDaClasseAsync(int igrejaId, int departamentoId);
        Task<AlunoDepartamento?> ObterPorIdAsync(int igrejaId, int departamentoId, int matriculaId);
        Task AtualizarAsync(AlunoDepartamento matricula);
    }
}