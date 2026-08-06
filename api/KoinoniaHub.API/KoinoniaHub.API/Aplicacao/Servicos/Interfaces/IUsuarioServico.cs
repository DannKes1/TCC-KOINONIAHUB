using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IUsuarioServico
    {
        Task<UsuarioRespostaDto> CriarParaPessoaAsync(int igrejaId, UsuarioCriarRequisicaoDto dto);
        Task<List<UsuarioRespostaDto>> ListarAsync(int igrejaId);
        Task<UsuarioRespostaDto?> ObterPorIdAsync(int igrejaId, int usuarioId);
        Task<bool> AtualizarAsync(int igrejaId, int usuarioId, int usuarioLogadoId, UsuarioAtualizarRequisicaoDto dto);
        Task<bool> ResetarSenhaAsync(int igrejaId, int usuarioId, UsuarioResetarSenhaRequisicaoDto dto);
    }
}