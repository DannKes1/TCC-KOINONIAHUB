using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IRelatorioEbdServico
    {
        Task<RankingFaltasRespostaDto> ObterRankingFaltasAsync(
            int igrejaId, int departamentoId, DateTime dataInicio, DateTime dataFim, int top);

        Task<FrequenciaTurmaRespostaDto> ObterFrequenciaTurmaAsync(int igrejaId, int departamentoId, DateTime dataInicio, DateTime dataFim);
        Task<ResumoDiaRespostaDto> ObterResumoDoDiaAsync(int igrejaId, DateTime data);

        Task<PainelAcompanhamentoRespostaDto> ObterPainelAcompanhamentoAsync(
            int igrejaId,
            int departamentoId,
            DateTime dataInicio,
            DateTime dataFim,
            decimal limiarAtencao,
            decimal limiarCritico,
            int faltasConsecutivasCritico);

        Task<MinhaFrequenciaTurmaRespostaDto> ObterMinhaFrequenciaTurmaAsync(
            int igrejaId,
            int usuarioId,
            int departamentoId,
            DateTime dataInicio,
            DateTime dataFim);
    }
}