namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AuthRespostaDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }

        public int IgrejaId { get; set; }
        public string NomeIgreja { get; set; } = string.Empty;

        public int UsuarioId { get; set; }
        public string EmailUsuario { get; set; } = string.Empty;
        public string Perfil { get; set; } = "Admin";
    }
}
