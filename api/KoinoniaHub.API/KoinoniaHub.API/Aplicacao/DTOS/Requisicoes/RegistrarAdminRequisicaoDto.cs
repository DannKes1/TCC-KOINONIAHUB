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
    }
}
