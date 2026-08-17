using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KoinoniaHub.API.Dominio.Entidades
{

    public class Usuario : EntidadeBase
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string SenhaHash { get; set; } = string.Empty;

        [StringLength(50)]
        public string Perfil { get; set; } = "Usuario";


        public bool Ativo { get; set; } = true;

        public DateTime? UltimoAcesso { get; set; }

        // Convite de primeiro acesso: guarda apenas o HASH (SHA-256) do token.
        // O token em claro é exibido uma única vez ao administrador, que o envia
        // à pessoa (ex.: WhatsApp). A própria pessoa define a senha pelo link.
        [StringLength(100)]
        public string? ConviteTokenHash { get; set; }

        public DateTime? ConviteExpiraEm { get; set; }

        // Chaves estrangeiras
        [ForeignKey("Igreja")]
        public int IgrejaId { get; set; }

        [ForeignKey("Pessoa")]
        public int? PessoaId { get; set; }

        public Igreja Igreja { get; set; } = null!;
        public Pessoa? Pessoa { get; set; }
    }
}
