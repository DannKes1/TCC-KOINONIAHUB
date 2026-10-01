namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    // RF38 / RNF 38.4: os totais somam apenas aulas Consolidadas; as Não realizadas e
    // as Em aberto com data já ocorrida (pendentes de fechamento) são apresentadas à parte.
    public class ResumoDiaRespostaDto
    {
        public DateTime Data { get; set; }
        public List<ResumoDiaTurmaDto> Turmas { get; set; } = new();

        // Somente aulas Consolidadas.
        public int TotalPresentes { get; set; }
        public int TotalAusentes { get; set; }
        public int TotalVisitantes { get; set; }

        public int TotalAulasConsolidadas { get; set; }
        public int TotalAulasNaoRealizadas { get; set; }
        public int TotalAulasPendentes { get; set; }

        public List<AulaResumidaRespostaDto> AulasPendentes { get; set; } = new();
        public List<AulaResumidaRespostaDto> AulasNaoRealizadas { get; set; } = new();
    }

    public class ResumoDiaTurmaDto
    {
        public int DepartamentoId { get; set; }
        public string Nome { get; set; } = string.Empty;

        // Há registros de presença em aula Consolidada da turma na data.
        public bool TemChamada { get; set; }

        // Somente aulas Consolidadas.
        public int Presentes { get; set; }
        public int Ausentes { get; set; }
        public int Visitantes { get; set; }

        // Situação das aulas da turma na data (RNF 38.4).
        public int AulasConsolidadas { get; set; }
        public int AulasNaoRealizadas { get; set; }
        public int AulasEmAberto { get; set; }

        // Há aula Em aberto e a data já ocorreu.
        public bool PendenteFechamento { get; set; }
    }
}
