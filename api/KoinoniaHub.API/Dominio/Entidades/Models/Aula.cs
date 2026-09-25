using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{

    public class Aula : EntidadeBase
    {
        [Required]
        public DateTime Data { get; set; }

        [StringLength(200)]
        public string? Tema { get; set; }

        [StringLength(2000)]
        public string? Conteudo { get; set; }

        [StringLength(1000)]
        public string? Observacoes { get; set; }

        // Situação da aula (RF31/RF33): EmAberto, Consolidada ou NaoRealizada.
        // Substitui o antigo booleano Consolidada (migration SituacaoAula).
        [Required]
        [StringLength(20)]
        public string Situacao { get; set; } = SituacaoAula.EmAberto;


        public int QuantidadeVisitantes { get; set; } = 0;

        [ForeignKey("Materia")]
        public int MateriaId { get; set; }

        [ForeignKey("Professor")]
        public int ProfessorId { get; set; }


        public Materia Materia { get; set; } = null!;
        public Pessoa Professor { get; set; } = null!;
        public ICollection<Presenca> Presencas { get; set; } = new List<Presenca>();
    }
}
