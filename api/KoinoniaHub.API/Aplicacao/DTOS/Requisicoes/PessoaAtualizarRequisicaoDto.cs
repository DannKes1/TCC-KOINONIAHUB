using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Aplicacao.Regras;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class PessoaAtualizarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(200, ErrorMessage = "O campo Nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        public DateTime? DataNascimento { get; set; }

        [StringLength(20, ErrorMessage = "O campo Sexo deve ter no máximo 20 caracteres.")]
        public string? Sexo { get; set; }

        [StringLength(50, ErrorMessage = "O campo Estado civil deve ter no máximo 50 caracteres.")]
        public string? EstadoCivil { get; set; }

        [StringLength(20, ErrorMessage = "O campo Celular deve ter no máximo 20 caracteres.")]
        public string? Celular { get; set; }

        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail deve ter no máximo 100 caracteres.")]
        public string? Email { get; set; }

        [StringLength(500, ErrorMessage = "O campo Endereço deve ter no máximo 500 caracteres.")]
        public string? Endereco { get; set; }

        [StringLength(100, ErrorMessage = "O campo Bairro deve ter no máximo 100 caracteres.")]
        public string? Bairro { get; set; }

        [StringLength(100, ErrorMessage = "O campo Cidade deve ter no máximo 100 caracteres.")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "O campo UF deve ter no máximo 2 caracteres.")]
        public string? Estado { get; set; }

        [StringLength(10, ErrorMessage = "O campo CEP deve ter no máximo 10 caracteres.")]
        [RegularExpression(Cep.Padrao, ErrorMessage = Cep.Mensagem)]
        public string? CEP { get; set; }

        [StringLength(20, ErrorMessage = "O campo Situação deve ter no máximo 20 caracteres.")]
        public string Situacao { get; set; } = "Ativo";
    }
}
