namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PresencaRespostaDto
    {
        public int Id { get; set; }
        public int AulaId { get; set; }

        public int AlunoDepartamentoId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public bool Presente { get; set; }
        public string? Observacao { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}