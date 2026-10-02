using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class PrimeiroAcessoAtivarRequisicaoDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;

        // RNF 40.5 / 42.1: versão do Termo de Uso e Sigilo aceita na ativação.
        // Sem a versão vigente, a API recusa com 400 e o convite não é consumido.
        [StringLength(20)]
        public string? AceiteTermoVersao { get; set; }
    }
}
