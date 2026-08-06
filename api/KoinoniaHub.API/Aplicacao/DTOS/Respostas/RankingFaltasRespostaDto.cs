namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class RankingFaltasRespostaDto
    {
        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public List<RankingFaltasItemRespostaDto> Itens { get; set; } = new();
    }
}