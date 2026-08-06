using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IChamadaServico
    {
        Task<List<PresencaRespostaDto>> RegistrarAsync(int igrejaId, int aulaId, ChamadaRegistrarRequisicaoDto dto);
        Task<List<PresencaRespostaDto>> ListarAsync(int igrejaId, int aulaId);
        Task<List<ItemChamadaCompletaRespostaDto>> ObterChamadaCompletaAsync(int igrejaId, int aulaId);
    }
}