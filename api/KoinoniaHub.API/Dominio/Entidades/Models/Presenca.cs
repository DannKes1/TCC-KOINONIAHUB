using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
   
    public class Presenca : EntidadeBase
    {
        public bool Presente { get; set; } = false;

        [StringLength(500)]
        public string? Observacao { get; set; }

        // Chaves estrangeiras
        [ForeignKey("Aula")]
        public int AulaId { get; set; }

        [ForeignKey("AlunoDepartamento")]
        public int AlunoDepartamentoId { get; set; }

        // Navegações
        public Aula Aula { get; set; } = null!;
        public AlunoDepartamento AlunoDepartamento { get; set; } = null!;
    }
}
