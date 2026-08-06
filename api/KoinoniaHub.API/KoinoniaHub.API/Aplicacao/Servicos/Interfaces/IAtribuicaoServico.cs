using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAtribuicaoServico
    {
        Task<AtribuicaoRespostaDto> CriarAsync(int igrejaId, AtribuicaoCriarRequisicaoDto dto);
        Task<List<AtribuicaoRespostaDto>> ListarAsync(int igrejaId);
        Task<List<AtribuicaoRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId, string? funcao = null, bool? ativo = null);
        Task<AtribuicaoRespostaDto?> ObterPorIdAsync(int igrejaId, int atribuicaoId);
        Task<bool> AtualizarAsync(int igrejaId, int atribuicaoId, AtribuicaoAtualizarRequisicaoDto dto);
        Task<bool> EncerrarAsync(int igrejaId, int atribuicaoId, DateTime? dataFim = null);
    }
}