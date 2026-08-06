namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class RankingFaltasItemRespostaDto
    {
        public int MatriculaId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public int TotalAulas { get; set; }
        public int Presentes { get; set; }
        public int FaltasTotais { get; set; } // ausentes marcados + não registrado
        public decimal PercentualPresenca { get; set; }
    }
}