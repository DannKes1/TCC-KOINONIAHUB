using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
    
    public class Departamento : EntidadeBase
    {
        [Required(ErrorMessage = "O nome do departamento é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50)]
        public string Tipo { get; set; } = "EBD";
        

        [StringLength(500)]
        public string? Descricao { get; set; }

        [StringLength(500)]
        public string? ImagemUrl { get; set; }

        public bool Ativo { get; set; } = true;

        // Chave estrangeira
        [ForeignKey("Igreja")]
        public int IgrejaId { get; set; }

       
        public Igreja Igreja { get; set; } = null!;
        public ICollection<Materia> Materias { get; set; } = new List<Materia>();
        public ICollection<Atribuicao> Atribuicoes { get; set; } = new List<Atribuicao>();
        public ICollection<AlunoDepartamento> Alunos { get; set; } = new List<AlunoDepartamento>();
    }
}