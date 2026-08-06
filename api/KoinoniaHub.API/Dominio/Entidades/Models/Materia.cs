using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
    
    public class Materia : EntidadeBase
    {
        [Required(ErrorMessage = "O nome da matéria é obrigatório")]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descricao { get; set; }

        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public int OrdemExibicao { get; set; } = 0;

        public bool Ativo { get; set; } = true;

        // Chave estrangeira
        [ForeignKey("Departamento")]
        public int DepartamentoId { get; set; }

        // Propriedades de navegação
        public Departamento Departamento { get; set; } = null!;
        public ICollection<Aula> Aulas { get; set; } = new List<Aula>();
    }
}