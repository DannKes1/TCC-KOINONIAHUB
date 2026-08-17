using KoinoniaHub.API.Aplicacao.DTOs.Respostas;

namespace KoinoniaHub.API.Aplicacao.Servicos.Interfaces
{
    public interface IPessoaImportacaoServico
    {
        Task<PessoaImportacaoRespostaDto> ImportarCsvAsync(int igrejaId, Stream conteudo);
    }
}
