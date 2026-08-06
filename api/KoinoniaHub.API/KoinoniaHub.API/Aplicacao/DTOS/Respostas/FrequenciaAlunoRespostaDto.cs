namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class FrequenciaAlunoRespostaDto
    {
        public int MatriculaId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public int TotalAulas { get; set; }
        public int Presentes { get; set; }
        public int AusentesMarcados { get; set; }
        public int NaoRegistrado { get; set; }

      
        /// Faltas efetivas: apenas as ausências marcadas em chamada. Aulas sem
        /// registro ficam em <see cref="NaoRegistrado"/> e não contam como falta.
       
        public int FaltasTotais => AusentesMarcados;

        public decimal PercentualPresenca { get; set; }
    }
}