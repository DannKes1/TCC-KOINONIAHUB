using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
    
    public class AlunoDepartamento : EntidadeBase
    {
        public DateTime DataMatricula { get; set; } = DateTime.UtcNow;
        public DateTime? DataSaida { get; set; }
        public bool Ativo { get; set; } = true;

        [StringLength(500)]
        public string? Observacao { get; set; }

        [ForeignKey("Pessoa")]
        public int PessoaId { get; set; }

        [ForeignKey("Departamento")]
        public int DepartamentoId { get; set; }

        public Pessoa Pessoa { get; set; } = null!;
        public Departamento Departamento { get; set; } = null!;

        public ICollection<Presenca> Presencas { get; set; } = new List<Presenca>();
    }
}
