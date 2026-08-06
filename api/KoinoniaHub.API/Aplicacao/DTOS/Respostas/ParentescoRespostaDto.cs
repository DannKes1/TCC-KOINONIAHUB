namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ParentescoRespostaDto
    {
        public int Id { get; set; }
        public int PessoaId { get; set; }
        public int ParenteId { get; set; }
        public string TipoRelacionamento { get; set; } = string.Empty;
        public string ParenteNome { get; set; } = string.Empty;
        public string? ParenteTelefone { get; set; }
        public string? ParenteCelular { get; set; }
    }
}