using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAulaServico
    {
        Task<AulaRespostaDto> CriarAsync(int igrejaId, AulaCriarRequisicaoDto dto);
        Task<List<AulaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
        Task<AulaRespostaDto?> ObterPorIdAsync(int igrejaId, int aulaId);

        // RF45. Devolvem null/false quando a aula não existe na igreja e lançam
        // InvalidOperationException quando a situação (45.2), a matéria ou o
        // professor (45.4) ou a existência de presenças (45.5) impedem a operação.
        Task<AulaRespostaDto?> AtualizarAsync(int igrejaId, int aulaId, AulaAtualizarRequisicaoDto dto);
        Task<bool> ExcluirAsync(int igrejaId, int aulaId);

        // RF33. Os três devolvem false quando a aula não existe na igreja e lançam
        // InvalidOperationException (ChamadaIncompletaException na consolidação)
        // quando a situação atual não permite a operação.
        Task<bool> ConsolidarAsync(int igrejaId, int aulaId);
        Task<bool> MarcarNaoRealizadaAsync(int igrejaId, int aulaId);
        Task<bool> ReabrirAsync(int igrejaId, int aulaId);
    }
}
