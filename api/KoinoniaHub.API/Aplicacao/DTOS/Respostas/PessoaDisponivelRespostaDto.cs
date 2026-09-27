namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PessoaDisponivelRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Situacao { get; set; } = "Ativo";
    }
}
