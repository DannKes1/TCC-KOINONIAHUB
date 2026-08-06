using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IPresencaHistoricoServico
    {
        Task<List<HistoricoPresencaRespostaDto>> ListarPorPessoaAsync(int igrejaId, int pessoaId);
    }
}