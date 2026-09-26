using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class PessoaCriarRequisicaoDto
    {
        [Required]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        public DateTime? DataNascimento { get; set; }

        [StringLength(20)]
        public string? Sexo { get; set; }

        [StringLength(50)]
        public string? EstadoCivil { get; set; }

        [StringLength(20)]
        public string? Celular { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Estado { get; set; }

        [StringLength(10)]
        public string? CEP { get; set; }

        [StringLength(20)]
        public string Situacao { get; set; } = "Ativo";
    }
}
