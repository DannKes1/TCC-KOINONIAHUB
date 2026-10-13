namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AulaRespostaDto
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string? Tema { get; set; }
       
        public string Situacao { get; set; } = string.Empty;

        public bool PendenteFechamento { get; set; }

        public int QuantidadeVisitantes { get; set; }

        public int MateriaId { get; set; }
        public string NomeMateria { get; set; } = string.Empty;

        // Etapa 6.4-B (aditivo): a turma da aula, para as telas de Chamada e de
        // Presenças voltarem à Página da Turma (RF20) sem uma consulta a mais.
        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public int ProfessorId { get; set; }
        public string NomeProfessor { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
    }
}
