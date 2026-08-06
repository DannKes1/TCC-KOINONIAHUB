using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
   
    public class Atribuicao : EntidadeBase
    {
        [Required]
        [StringLength(50)]
        public string Funcao { get; set; } = string.Empty;
        // Valores: Professor, Auxiliar

        public DateTime DataInicio { get; set; } = DateTime.UtcNow;

        public DateTime? DataFim { get; set; }

        public bool Ativo { get; set; } = true;

        // Chaves estrangeiras
        [ForeignKey("Pessoa")]
        public int PessoaId { get; set; }

        [ForeignKey("Departamento")]
        public int DepartamentoId { get; set; }


        public Pessoa Pessoa { get; set; } = null!;
        public Departamento Departamento { get; set; } = null!;
    }
}