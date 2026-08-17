namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class ConviteRespostaDto
    {
        public int UsuarioId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? NomePessoa { get; set; }

        // Token em claro: exibido somente nesta resposta.
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }
}
