using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class DepartamentoAtualizarRequisicaoDto
    {
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        public string Tipo { get; set; } = "EBD";

        [StringLength(500)]
        public string? Descricao { get; set; }

        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public bool Ativo { get; set; } = true;
    }
}