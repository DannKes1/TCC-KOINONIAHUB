using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;
using KoinoniaHub.API.Aplicacao.DTOS.Requisicoes;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IPessoaServico
    {
        Task<PessoaRespostaDto> CriarAsync(int igrejaId, PessoaCriarRequisicaoDto dto);
        Task<List<PessoaRespostaDto>> ListarAsync(int igrejaId);
        Task<PessoaRespostaDto?> ObterPorIdAsync(int igrejaId, int pessoaId);
        Task<bool> AtualizarAsync(int igrejaId, int pessoaId, PessoaAtualizarRequisicaoDto dto);
        Task<List<MinhaTurmaRespostaDto>> ListarMinhasTurmasAsync(int igrejaId, int usuarioId);
        Task<PessoaRespostaDto?> ObterMeusDadosAsync(int igrejaId, int usuarioId);
        Task<bool> AtualizarMeusDadosAsync(int igrejaId, int usuarioId, MeusDadosAtualizarRequisicaoDto dto);
    }
}
