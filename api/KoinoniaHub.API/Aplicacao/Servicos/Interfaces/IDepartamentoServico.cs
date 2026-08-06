using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IDepartamentoServico
    {
        Task<DepartamentoRespostaDto> CriarAsync(int igrejaId, DepartamentoCriarRequisicaoDto dto);
        Task<DepartamentoRespostaDto?> ObterPorIdAsync(int igrejaId, int departamentoId);
        Task<bool> AtualizarAsync(int igrejaId, int departamentoId, DepartamentoAtualizarRequisicaoDto dto);
        Task<List<DepartamentoRespostaDto>> ListarAsync(int igrejaId, int usuarioId, string perfil);
    }
}