namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class MateriaRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public int OrdemExibicao { get; set; }

        public int DepartamentoId { get; set; }
        public string NomeDepartamento { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}