namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class FrequenciaTurmaRespostaDto
    {
        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public int TotalAulas { get; set; }
        public int TotalAlunos { get; set; }

        public int TotalPresentes { get; set; }
        public int TotalAusentesMarcados { get; set; }
        public int TotalNaoRegistrado { get; set; }

        public decimal PercentualPresencaGeral { get; set; }

        public List<FrequenciaAlunoRespostaDto> Alunos { get; set; } = new();
        public List<FrequenciaAulaRespostaDto> Aulas { get; set; } = new();
    }
}