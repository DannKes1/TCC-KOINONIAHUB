using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class UsuarioCriarRequisicaoDto
    {
        [Required]
        public int PessoaId { get; set; }

        // Se não informar, usa o e-mail da Pessoa.
        public string? Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        
        public string Perfil { get; set; } = "Usuario";
    }
}