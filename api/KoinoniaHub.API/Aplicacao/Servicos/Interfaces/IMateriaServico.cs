using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IMateriaServico
    {
        Task<MateriaRespostaDto> CriarAsync(int igrejaId, MateriaCriarRequisicaoDto dto);
        Task<List<MateriaRespostaDto>> ListarPorDepartamentoAsync(int igrejaId, int departamentoId);
        Task<MateriaRespostaDto?> ObterPorIdAsync(int igrejaId, int materiaId);
        Task<bool> AtualizarAsync(int igrejaId, int materiaId, MateriaAtualizarRequisicaoDto dto);
    }
}