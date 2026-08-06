namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{

    public class MinhaFrequenciaTurmaRespostaDto
    {
        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public int TotalAulas { get; set; }
        public int Presentes { get; set; }
        public int AusentesMarcados { get; set; }
        public int NaoRegistrado { get; set; }
        public decimal PercentualPresenca { get; set; }

        public List<MinhaFrequenciaAulaRespostaDto> Aulas { get; set; } = new();
    }
}