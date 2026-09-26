using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
    public class AceiteTermo
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Usuario")]
        public int UsuarioId { get; set; }

        [ForeignKey("Igreja")]
        public int IgrejaId { get; set; }

        [Required]
        [StringLength(20)]
        public string TermoVersao { get; set; } = string.Empty;

        [Required]
        [StringLength(64)]
        public string TermoHash { get; set; } = string.Empty;

        public DateTime AceitoEm { get; set; }

        [StringLength(45)]
        public string? Ip { get; set; }

        [Required]
        [StringLength(30)]
        public string Meio { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        public Usuario Usuario { get; set; } = null!;
        public Igreja Igreja { get; set; } = null!;
    }
}