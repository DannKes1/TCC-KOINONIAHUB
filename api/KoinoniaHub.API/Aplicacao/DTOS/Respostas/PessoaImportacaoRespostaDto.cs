namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PessoaImportacaoRespostaDto
    {
        public int TotalLinhas { get; set; }
        public int Criados { get; set; }
        public int Ignorados { get; set; }
        public int Erros { get; set; }

        public List<PessoaImportacaoItemDto> Itens { get; set; } = new();
    }

    public class PessoaImportacaoItemDto
    {
        public int Linha { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }

        // "Criado" | "Ignorado" | "Erro"
        public string Status { get; set; } = "Erro";
        public string? Mensagem { get; set; }
    }
}
