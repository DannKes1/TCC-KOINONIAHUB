using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class AulaCriarRequisicaoDto
    {
        // Anulável para o [Required] valer (S6964): sem o campo, a aula era gravada
        // com 0001-01-01; agora a requisição volta 400.
        [Required(ErrorMessage = "Informe a data da aula.")]
        public DateTime? Data { get; set; }

        [StringLength(200)]
        public string? Tema { get; set; }

        [StringLength(2000)]
        public string? Conteudo { get; set; }

        [StringLength(1000)]
        public string? Observacoes { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Informe a matéria.")]
        public int MateriaId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Informe o professor responsável.")]
        public int ProfessorId { get; set; } // PessoaId do professor
    }
}