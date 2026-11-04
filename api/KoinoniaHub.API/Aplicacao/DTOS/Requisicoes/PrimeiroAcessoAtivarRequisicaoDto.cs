using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class PrimeiroAcessoAtivarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Token é obrigatório.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Nova senha é obrigatório.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;

        // RNF 40.5 / 42.1: versão do Termo de Uso e Sigilo aceita na ativação.
        // Sem a versão vigente, a API recusa com 400 e o convite não é consumido.
        [StringLength(20, ErrorMessage = "O campo Versão do termo deve ter no máximo 20 caracteres.")]
        public string? AceiteTermoVersao { get; set; }
    }
}
