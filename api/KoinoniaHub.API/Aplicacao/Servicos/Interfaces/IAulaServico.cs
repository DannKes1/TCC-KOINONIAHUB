using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAulaServico
    {
        Task<AulaRespostaDto> CriarAsync(int igrejaId, AulaCriarRequisicaoDto dto);
        Task<List<AulaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
        Task<AulaRespostaDto?> ObterPorIdAsync(int igrejaId, int aulaId);

        // RF33. Os três devolvem false quando a aula não existe na igreja e lançam
        // InvalidOperationException (ChamadaIncompletaException na consolidação)
        // quando a situação atual não permite a operação.
        Task<bool> ConsolidarAsync(int igrejaId, int aulaId);
        Task<bool> MarcarNaoRealizadaAsync(int igrejaId, int aulaId);
        Task<bool> ReabrirAsync(int igrejaId, int aulaId);
    }
}
