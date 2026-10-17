using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IParentescoRepositorio
    {
        Task<Parentesco> CriarAsync(Parentesco parentesco);
        Task<List<Parentesco>> ListarDaPessoaAsync(int igrejaId, int pessoaId);
        // Vínculos registrados no cadastro de outras pessoas em que esta pessoa é o parente
        // (origem dos vínculos derivados, RNF 12.3). Vem com a pessoa de origem carregada.
        Task<List<Parentesco>> ListarComoParenteAsync(int igrejaId, int pessoaId);
        Task<Parentesco?> ObterPorIdAsync(int igrejaId, int pessoaId, int parentescoId);
        // Registro em que a pessoa da rota é o parente (lado derivado), para recusar a remoção (12.3).
        Task<Parentesco?> ObterComoParenteAsync(int igrejaId, int pessoaId, int parentescoId);
        Task RemoverAsync(Parentesco parentesco);
    }
}
