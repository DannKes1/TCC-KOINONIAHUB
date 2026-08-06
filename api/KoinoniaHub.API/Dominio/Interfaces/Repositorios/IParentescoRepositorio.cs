using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IParentescoRepositorio
    {
        Task<Parentesco> CriarAsync(Parentesco parentesco);
        Task<List<Parentesco>> ListarDaPessoaAsync(int igrejaId, int pessoaId);
        Task<Parentesco?> ObterPorIdAsync(int igrejaId, int pessoaId, int parentescoId);
        Task RemoverAsync(Parentesco parentesco);
    }
}