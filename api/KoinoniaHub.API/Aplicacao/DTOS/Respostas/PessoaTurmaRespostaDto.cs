namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class PessoaTurmaRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Situacao { get; set; } = "Ativo";
        public string? Celular { get; set; }
        public List<PessoaTurmaParentescoRespostaDto> Parentescos { get; set; } = new();
    }

    public class PessoaTurmaParentescoRespostaDto
    {
        public string ParenteNome { get; set; } = string.Empty;
        public string TipoRelacionamento { get; set; } = string.Empty;
        public string? ParenteCelular { get; set; }
    }
}