namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ItemChamadaCompletaRespostaDto
    {
        public int AlunoDepartamentoId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public bool Presente { get; set; }
        public string? Observacao { get; set; }
    }
}