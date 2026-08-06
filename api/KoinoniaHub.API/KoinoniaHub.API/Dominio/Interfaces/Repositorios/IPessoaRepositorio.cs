using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IPessoaRepositorio
    {
        Task<Pessoa> CriarAsync(Pessoa pessoa);
        Task<Pessoa?> ObterPorIdAsync(int igrejaId, int pessoaId);
        Task<List<Pessoa>> ListarAsync(int igrejaId);
        Task AtualizarAsync(Pessoa pessoa);
    }
}
