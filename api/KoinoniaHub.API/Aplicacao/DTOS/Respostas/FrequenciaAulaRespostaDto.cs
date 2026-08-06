namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class FrequenciaAulaRespostaDto
    {
        public int AulaId { get; set; }
        public DateTime Data { get; set; }
        public string? Tema { get; set; }

        public int TotalAlunos { get; set; }
        public int Presentes { get; set; }
        public int AusentesMarcados { get; set; }
        public int NaoRegistrado { get; set; }

        public decimal PercentualPresenca { get; set; }
    }
}