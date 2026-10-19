using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    // Períodos e dia em datas civis da igreja (RelogioIgreja, corretiva da 7.2): o
    // serviço converte cada dia local nos instantes UTC que o banco guarda.
    public interface IRelatorioEbdServico
    {
        Task<RankingFaltasRespostaDto> ObterRankingFaltasAsync(
            int igrejaId, int departamentoId, DateOnly dataInicio, DateOnly dataFim, int top);

        Task<FrequenciaTurmaRespostaDto> ObterFrequenciaTurmaAsync(int igrejaId, int departamentoId, DateOnly dataInicio, DateOnly dataFim);
        Task<ResumoDiaRespostaDto> ObterResumoDoDiaAsync(int igrejaId, DateOnly data);

        Task<PainelAcompanhamentoRespostaDto> ObterPainelAcompanhamentoAsync(
            int igrejaId,
            int departamentoId,
            DateOnly dataInicio,
            DateOnly dataFim,
            decimal limiarAtencao,
            decimal limiarCritico,
            int faltasConsecutivasCritico);

        Task<MinhaFrequenciaTurmaRespostaDto> ObterMinhaFrequenciaTurmaAsync(
            int igrejaId,
            int usuarioId,
            int departamentoId,
            DateOnly dataInicio,
            DateOnly dataFim);
    }
}
