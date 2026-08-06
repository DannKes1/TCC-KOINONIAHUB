namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    
    public class PainelAcompanhamentoRespostaDto
    {
        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public int TotalAulas { get; set; }
        public int TotalAlunos { get; set; }

        
        public decimal LimiarAtencao { get; set; }
        public decimal LimiarCritico { get; set; }
        public int FaltasConsecutivasCritico { get; set; }

        // Resumo
        public int TotalCritico { get; set; }
        public int TotalAtencao { get; set; }

        public List<AlunoEmAtencaoRespostaDto> Alunos { get; set; } = new();
    }
}