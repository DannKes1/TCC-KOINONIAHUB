namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class DepartamentoRespostaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = "EBD";
        public bool Ativo { get; set; }

        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}