namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class MinhaTurmaRespostaDto
    {
        public int DepartamentoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Ativo { get; set; }

      
        // "Aluno" (matriculado) ou nome da função na atribuição (Professor/Auxiliar)
        public string Vinculo { get; set; } = string.Empty;

        // Professor(es) com atribuição ativa na turma
        public string? Responsavel { get; set; }
    }
}