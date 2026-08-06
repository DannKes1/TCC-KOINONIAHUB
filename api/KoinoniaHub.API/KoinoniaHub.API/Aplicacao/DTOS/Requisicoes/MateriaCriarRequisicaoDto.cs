using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class MateriaCriarRequisicaoDto
    {
        [Required]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public int OrdemExibicao { get; set; } = 0;

        public bool Ativo { get; set; } = true;

        [Required]
        public int DepartamentoId { get; set; }
    }
}