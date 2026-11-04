using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Aplicacao.Regras;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class IgrejaCriarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(200, ErrorMessage = "O campo Nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "O campo Endereço deve ter no máximo 500 caracteres.")]
        public string? Endereco { get; set; }

        [StringLength(100, ErrorMessage = "O campo Cidade deve ter no máximo 100 caracteres.")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "O campo UF deve ter no máximo 2 caracteres.")]
        public string? Estado { get; set; }

        [StringLength(10, ErrorMessage = "O campo CEP deve ter no máximo 10 caracteres.")]
        [RegularExpression(Cep.Padrao, ErrorMessage = Cep.Mensagem)]
        public string? CEP { get; set; }

        [StringLength(20, ErrorMessage = "O campo Telefone deve ter no máximo 20 caracteres.")]
        public string? Telefone { get; set; }

        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail deve ter no máximo 100 caracteres.")]
        public string? Email { get; set; }

        [StringLength(18, ErrorMessage = "O campo CNPJ deve ter no máximo 18 caracteres.")]
        public string? CNPJ { get; set; }

        [StringLength(500, ErrorMessage = "O campo Logo (URL) deve ter no máximo 500 caracteres.")]
        public string? LogoUrl { get; set; }
    }
}
