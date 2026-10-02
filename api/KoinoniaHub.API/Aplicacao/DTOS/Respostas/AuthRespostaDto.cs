using System.Text.Json.Serialization;

namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class AuthRespostaDto
    {
        // O JWT é entregue ao navegador somente no cookie httpOnly "kh_token"
        // (monografia, seção 4.8): esta propriedade existe para o controller
        // gravar o cookie e nunca é serializada no corpo da resposta.
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiraEm { get; set; }

        public int IgrejaId { get; set; }
        public string NomeIgreja { get; set; } = string.Empty;

        public int UsuarioId { get; set; }
        public string EmailUsuario { get; set; } = string.Empty;
        public string Perfil { get; set; } = "Admin";

        // Sempre false após o cadastro inicial (o aceite é gravado na mesma transação);
        // existe para o front tratar login e cadastro com o mesmo contrato.
        public bool TermoPendente { get; set; }
    }
}
