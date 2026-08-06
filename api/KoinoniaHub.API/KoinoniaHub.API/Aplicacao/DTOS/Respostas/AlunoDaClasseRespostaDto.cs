namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AlunoDaClasseRespostaDto
    {
        public int MatriculaId { get; set; }
        public int PessoaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string StatusPessoa { get; set; } = "Ativo";
        public bool MatriculaAtiva { get; set; }
        public DateTime DataMatricula { get; set; }
    }
}