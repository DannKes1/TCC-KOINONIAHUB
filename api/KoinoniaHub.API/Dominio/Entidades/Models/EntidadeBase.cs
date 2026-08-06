using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Dominio.Entidades
{
    public abstract class EntidadeBase
    {
        [Key]
        public int Id { get; set; }

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; set; }
    }
}
