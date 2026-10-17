namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ParentescoRespostaDto
    {
        public int Id { get; set; }
        public int PessoaId { get; set; }
        public int ParenteId { get; set; }
        public string TipoRelacionamento { get; set; } = string.Empty;
        public string ParenteNome { get; set; } = string.Empty;
        public string? ParenteCelular { get; set; }
        // RNF 12.3: true quando a linha é o grau inverso de um vínculo registrado no cadastro
        // do parente. Nesse caso Id é o do registro de origem e a remoção só é feita por lá.
        public bool Derivado { get; set; }
    }
}
