namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    // Aula listada à parte nos relatórios (Plano 6.9): pendente de fechamento
    // (Em aberto com data já ocorrida) ou Não realizada. Não entra nos cálculos.
    public class AulaResumidaRespostaDto
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }
        public string Materia { get; set; } = string.Empty;
        public string Professor { get; set; } = string.Empty;
        public int DepartamentoId { get; set; }
        public string Departamento { get; set; } = string.Empty;
    }
}
