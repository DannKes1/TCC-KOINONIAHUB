namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
   
    public class AlunoEmAtencaoRespostaDto
    {
        public int MatriculaId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public int TotalAulas { get; set; }
        public int Presentes { get; set; }
        public int FaltasTotais { get; set; } // ausências efetivamente marcadas em chamada
        public decimal PercentualPresenca { get; set; }

       
        public int FaltasConsecutivas { get; set; }

       
        public DateTime? DataUltimaPresenca { get; set; }

        // ----- Classificação -----

        public string Classificacao { get; set; } = "Atencao";

      
        public List<string> Motivos { get; set; } = new();
    }
}