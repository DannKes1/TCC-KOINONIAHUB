using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class RegistrarAdminRequisicaoDto
    {
        [Required]
        public IgrejaCriarRequisicaoDto Igreja { get; set; } = new();

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EmailAdmin { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string SenhaAdmin { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string NomeAdmin { get; set; } = string.Empty;

        // RNF 1.5 / 42.1: versão do Termo de Uso e Sigilo aceita no cadastro inicial.
        // Sem a versão vigente, a API recusa com 400 e nada é criado.
        [StringLength(20)]
        public string? AceiteTermoVersao { get; set; }
    }
}
