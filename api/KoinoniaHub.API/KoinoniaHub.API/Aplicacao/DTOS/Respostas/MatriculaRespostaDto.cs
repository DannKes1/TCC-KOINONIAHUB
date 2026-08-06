namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class MatriculaRespostaDto
    {
        public int Id { get; set; }
        public int PessoaId { get; set; }
        public string NomePessoa { get; set; } = string.Empty;

        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public bool Ativo { get; set; }
        public DateTime DataMatricula { get; set; }
        public DateTime? DataSaida { get; set; }

        public string? Observacao { get; set; }
    }
}