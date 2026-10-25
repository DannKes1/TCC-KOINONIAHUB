using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class MatriculaCriarRequisicaoDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Informe o aluno.")]
        public int PessoaId { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }
    }
}