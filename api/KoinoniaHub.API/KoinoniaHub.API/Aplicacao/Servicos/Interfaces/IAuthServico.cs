using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAuthServico
    {
        Task<AuthRespostaDto> RegistrarAdminAsync(RegistrarAdminRequisicaoDto dto);
        Task<LoginRespostaDto> LoginAsync(LoginRequisicaoDto dto);


    }
}
