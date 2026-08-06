using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IParentescoServico
    {
        Task<ParentescoRespostaDto> AdicionarAsync(int igrejaId, int pessoaId, ParentescoCriarRequisicaoDto dto);
        Task<List<ParentescoRespostaDto>> ListarAsync(int igrejaId, int pessoaId);
        Task<bool> RemoverAsync(int igrejaId, int pessoaId, int parentescoId);
    }
}