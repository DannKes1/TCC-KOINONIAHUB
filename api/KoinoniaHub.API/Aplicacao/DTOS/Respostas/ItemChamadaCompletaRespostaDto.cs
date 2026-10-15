namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ItemChamadaCompletaRespostaDto
    {
        public int AlunoDepartamentoId { get; set; }
        public int PessoaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;

        public bool Presente { get; set; }
        public string? Observacao { get; set; }

        // Etapa 6.4-C (aditivo): indica se já existe registro explícito de presença ou
        // ausência para a matrícula nesta aula (RNFs 32.6/33.3). Sem registro, Presente
        // vem false por padrão — e a tela precisa distinguir "ausente" de "não lançado".
        public bool Registrado { get; set; }
    }
}