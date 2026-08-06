namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AulaRespostaDto
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string? Tema { get; set; }
        public bool Consolidada { get; set; }
        public int QuantidadeVisitantes { get; set; }

        public int MateriaId { get; set; }
        public string NomeMateria { get; set; } = string.Empty;

        public int ProfessorId { get; set; }
        public string NomeProfessor { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
    }
}