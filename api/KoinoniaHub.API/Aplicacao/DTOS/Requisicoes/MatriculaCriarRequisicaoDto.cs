using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class MatriculaCriarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Pessoa é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Informe o aluno.")]
        public int PessoaId { get; set; }

        [StringLength(500, ErrorMessage = "O campo Observação deve ter no máximo 500 caracteres.")]
        public string? Observacao { get; set; }
    }
}