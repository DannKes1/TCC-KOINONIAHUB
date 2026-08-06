namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AtribuicaoRespostaDto
    {
        public int Id { get; set; }

        public int PessoaId { get; set; }
        public string PessoaNome { get; set; } = string.Empty;

        public int DepartamentoId { get; set; }
        public string DepartamentoNome { get; set; } = string.Empty;

        public string Funcao { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; }
    }
}