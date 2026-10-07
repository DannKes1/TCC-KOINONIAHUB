namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PessoaImportacaoRespostaDto
    {
        public int TotalLinhas { get; set; }
        public int Criados { get; set; }
        public int Ignorados { get; set; }
        public int Erros { get; set; }

        // RNF 41.3: linhas ignoradas por nome repetido sem e-mail, sinalizadas para conferência.
        public int ParaConferencia { get; set; }

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

        // RNF 41.3: true quando a linha foi ignorada por coincidência de nome sem e-mail e
        // precisa ser conferida pelo usuário (possível homônimo a cadastrar manualmente).
        public bool ParaConferencia { get; set; }
    }
}
