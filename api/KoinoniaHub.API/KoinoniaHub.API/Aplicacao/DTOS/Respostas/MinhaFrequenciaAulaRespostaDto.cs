namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{

    public class MinhaFrequenciaAulaRespostaDto
    {
        public int AulaId { get; set; }
        public DateTime Data { get; set; }
        public string? Tema { get; set; }

        
        public string Situacao { get; set; } = string.Empty;
    }
}