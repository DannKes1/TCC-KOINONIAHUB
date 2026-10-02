namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    // GET /api/termo/vigente: texto fixo da versão (hash) e, quando identificável,
    // a igreja para exibição dinâmica do cabeçalho (RNF 42.7).
    public class TermoVigenteRespostaDto
    {
        public string Versao { get; set; } = string.Empty;
        public DateTime VigenteDesde { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public TermoIgrejaRespostaDto? Igreja { get; set; }
    }

    public class TermoIgrejaRespostaDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
    }
}
