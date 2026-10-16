using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    // RF45 / CSU09 FA1: os mesmos campos do cadastro que o usuário pode alterar
    // numa aula Em aberto. Situação, visitantes e observações não entram aqui —
    // pertencem à chamada (RF32) e ao fechamento (RF33).
    public class AulaAtualizarRequisicaoDto
    {
        [Required]
        public DateTime Data { get; set; }

        [StringLength(200)]
        public string? Tema { get; set; }

        [StringLength(2000)]
        public string? Conteudo { get; set; }

        [Required]
        public int MateriaId { get; set; }

        [Required]
        public int ProfessorId { get; set; } // PessoaId do professor
    }
}
