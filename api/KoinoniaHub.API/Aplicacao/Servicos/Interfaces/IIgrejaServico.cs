using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IIgrejaServico
    {
        Task<IgrejaRespostaDto> CriarAsync(IgrejaCriarRequisicaoDto dto);
        Task<IgrejaRespostaDto?> ObterPorIdAsync(int id);
    }
}
