using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAulaServico
    {
        Task<AulaRespostaDto> CriarAsync(int igrejaId, AulaCriarRequisicaoDto dto);
        Task<List<AulaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
        Task<AulaRespostaDto?> ObterPorIdAsync(int igrejaId, int aulaId);

        Task<bool> ConsolidarAsync(int igrejaId, int aulaId);
    }
}