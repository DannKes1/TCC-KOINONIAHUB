using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class AulaCriarRequisicaoDto
    {
        [Required]
        public DateTime Data { get; set; }

        [StringLength(200)]
        public string? Tema { get; set; }

        [StringLength(2000)]
        public string? Conteudo { get; set; }

        [StringLength(1000)]
        public string? Observacoes { get; set; }

        [Required]
        public int MateriaId { get; set; }

        [Required]
        public int ProfessorId { get; set; } // PessoaId do professor
    }
}