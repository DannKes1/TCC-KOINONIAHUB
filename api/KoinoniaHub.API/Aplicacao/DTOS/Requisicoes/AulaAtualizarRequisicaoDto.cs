using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    // RF45 / CSU09 FA1: os mesmos campos do cadastro que o usuário pode alterar
    // numa aula Em aberto. Situação, visitantes e observações não entram aqui —
    // pertencem à chamada (RF32) e ao fechamento (RF33).
    public class AulaAtualizarRequisicaoDto
    {
        // Anulável para o [Required] valer (S6964), como no cadastro.
        [Required(ErrorMessage = "Informe a data da aula.")]
        public DateTime? Data { get; set; }

        [StringLength(200)]
        public string? Tema { get; set; }

        [StringLength(2000)]
        public string? Conteudo { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Informe a matéria.")]
        public int MateriaId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Informe o professor responsável.")]
        public int ProfessorId { get; set; } // PessoaId do professor
    }
}
