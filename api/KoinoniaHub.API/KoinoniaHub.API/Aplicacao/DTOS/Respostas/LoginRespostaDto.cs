namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class LoginRespostaDto
    {
        public int? PessoaId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }

        public int UsuarioId { get; set; }
        public string EmailUsuario { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;

        public int IgrejaId { get; set; }
    }
}
