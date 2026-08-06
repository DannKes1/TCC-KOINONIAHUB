using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Dominio.Interfaces.Repositorios
{
    public interface IAtribuicaoRepositorio
    {
        Task<Atribuicao> CriarAsync(Atribuicao atribuicao);
        Task<List<Atribuicao>> ListarAsync(int igrejaId);
        Task<List<Atribuicao>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId, string? funcao = null, bool? ativo = null);
        Task<Atribuicao?> ObterPorIdAsync(int igrejaId, int atribuicaoId);
        Task AtualizarAsync(Atribuicao atribuicao);
    }
}