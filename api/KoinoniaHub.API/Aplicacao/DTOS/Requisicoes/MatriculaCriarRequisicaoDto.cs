using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class MatriculaCriarRequisicaoDto
    {
        [Required]
        public int PessoaId { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }
    }
}