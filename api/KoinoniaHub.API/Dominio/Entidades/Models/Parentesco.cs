using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
    
    public class Parentesco : EntidadeBase
    {
        [Required]
        [StringLength(50)]
        public string TipoRelacionamento { get; set; } = string.Empty;
        

        // Chaves estrangeiras
        [ForeignKey("Pessoa")]
        public int PessoaId { get; set; }

        [ForeignKey("Parente")]
        public int ParenteId { get; set; }

        // Propriedades de navegação
        public Pessoa Pessoa { get; set; } = null!;
        public Pessoa Parente { get; set; } = null!;
    }
}