namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class IgrejaRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Email { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
