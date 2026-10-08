namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class IgrejaRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Email { get; set; }
        // RF44: telefone entra na resposta para a tela de edição (e é o 2º canal do termo, RNF 42.7).
        public string? Telefone { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
