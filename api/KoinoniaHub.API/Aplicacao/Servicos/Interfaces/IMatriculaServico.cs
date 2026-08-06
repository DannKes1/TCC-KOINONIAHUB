using KoinoniaHub.API.Aplicacao.DTOs.Requisicoes;
using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IMatriculaServico
    {
        Task<MatriculaRespostaDto> MatricularAsync(int igrejaId, int departamentoId, MatriculaCriarRequisicaoDto dto);
        Task<List<AlunoDaClasseRespostaDto>> ListarAlunosDaClasseAsync(int igrejaId, int departamentoId);
        Task<bool> InativarMatriculaAsync(int igrejaId, int departamentoId, int matriculaId);
        Task<List<PessoaRespostaDto>> ListarPessoasDisponiveisAsync(int igrejaId, int departamentoId);
    }
}