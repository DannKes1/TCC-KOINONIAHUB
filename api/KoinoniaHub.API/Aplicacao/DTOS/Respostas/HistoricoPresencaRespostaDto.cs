namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class HistoricoPresencaRespostaDto
    {
        public int AulaId { get; set; }
        public DateTime DataAula { get; set; }

        public int DepartamentoId { get; set; }
        public string DepartamentoNome { get; set; } = string.Empty;

        public int MateriaId { get; set; }
        public string MateriaNome { get; set; } = string.Empty;

        public bool Presente { get; set; }
        public string? Observacao { get; set; }
    }
}