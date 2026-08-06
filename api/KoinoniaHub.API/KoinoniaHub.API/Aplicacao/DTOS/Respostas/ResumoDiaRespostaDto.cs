namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ResumoDiaRespostaDto
    {
        public DateTime Data { get; set; }
        public List<ResumoDiaTurmaDto> Turmas { get; set; } = new();
        public int TotalPresentes { get; set; }
        public int TotalAusentes { get; set; }
        public int TotalVisitantes { get; set; }
    }

    public class ResumoDiaTurmaDto
    {
        public int DepartamentoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool TemChamada { get; set; }
        public int Presentes { get; set; }
        public int Ausentes { get; set; }
        public int Visitantes { get; set; }
    }
}
