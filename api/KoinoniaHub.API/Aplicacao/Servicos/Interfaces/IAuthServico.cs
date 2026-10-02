using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IAuthServico
    {
        // ipOrigem: só para o registro de aceite do termo (RNF 42.8).
        Task<AuthRespostaDto> RegistrarAdminAsync(RegistrarAdminRequisicaoDto dto, string? ipOrigem);
        Task<LoginRespostaDto> LoginAsync(LoginRequisicaoDto dto);

        // Primeiro acesso por convite (endpoints públicos)
        Task<PrimeiroAcessoValidarRespostaDto?> ValidarConviteAsync(string token);
        Task<PrimeiroAcessoValidarRespostaDto> AtivarPrimeiroAcessoAsync(PrimeiroAcessoAtivarRequisicaoDto dto, string? ipOrigem);
    }
}
