using System.Text.Json.Serialization;

namespace KoinoniaHub.API.Aplicacao.DTOs.Respostas
{
    public class LoginRespostaDto
    {
        public int? PessoaId { get; set; }

        // O JWT é entregue ao navegador somente no cookie httpOnly "kh_token"
        // (monografia, seção 4.8): esta propriedade existe para o controller
        // gravar o cookie e nunca é serializada no corpo da resposta.
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiraEm { get; set; }

        public int UsuarioId { get; set; }
        public string EmailUsuario { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;

        public int IgrejaId { get; set; }

        // RNF 2.5 / 13.4: não há aceite da versão vigente do termo para a conta; o
        // front exige o aceite antes de liberar qualquer outra tela.
        public bool TermoPendente { get; set; }
    }
}
