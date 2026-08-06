using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{
   
    public class Pessoa : EntidadeBase
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(14)]
        public string? CPF { get; set; }

        public DateTime? DataNascimento { get; set; }

        [StringLength(20)]
        public string? Sexo { get; set; }

        [StringLength(50)]
        public string? EstadoCivil { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(20)]
        public string? Celular { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Estado { get; set; }

        [StringLength(10)]
        public string? CEP { get; set; }

        
        [StringLength(20)]
        public string Situacao { get; set; } = "Ativo";

        
        [StringLength(20)]
        public string Categoria { get; set; } = "Membro";

 
        public DateTime? DataInativacao { get; set; }

        public DateTime? DataBatismo { get; set; }

        public DateTime? DataMembresia { get; set; }

        [StringLength(500)]
        public string? FotoUrl { get; set; }

        [StringLength(1000)]
        public string? Observacoes { get; set; }

        // Chave estrangeira
        [ForeignKey("Igreja")]
        public int IgrejaId { get; set; }

        // Propriedades de navegação
        public Igreja Igreja { get; set; } = null!;
        public Usuario? Usuario { get; set; }
        public ICollection<Parentesco> Parentescos { get; set; } = new List<Parentesco>();
        public ICollection<Parentesco> ParentescosComoParente { get; set; } = new List<Parentesco>();
        public ICollection<Atribuicao> Atribuicoes { get; set; } = new List<Atribuicao>();
        public ICollection<AlunoDepartamento> Matriculas { get; set; } = new List<AlunoDepartamento>();
        public ICollection<Aula> AulasMinistradas { get; set; } = new List<Aula>();
    }
}