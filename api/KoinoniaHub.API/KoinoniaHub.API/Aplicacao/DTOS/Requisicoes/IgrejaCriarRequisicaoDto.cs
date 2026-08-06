using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class IgrejaCriarRequisicaoDto
    {
        [Required]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Estado { get; set; }

        [StringLength(10)]
        public string? CEP { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(18)]
        public string? CNPJ { get; set; }

        [StringLength(500)]
        public string? LogoUrl { get; set; }
    }
}
