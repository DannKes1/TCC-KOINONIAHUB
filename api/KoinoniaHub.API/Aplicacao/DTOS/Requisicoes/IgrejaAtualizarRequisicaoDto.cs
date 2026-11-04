using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    // RF44 — Editar Dados da Igreja: os cinco campos do requisito. Endereco, CEP, CNPJ e
    // LogoUrl existem na entidade, mas ficam fora por decisão do autor (motivação do RF é
    // o canal de contato exibido no Termo de Uso e Sigilo, RNF 42.7).
    public class IgrejaAtualizarRequisicaoDto
    {
        [Required(ErrorMessage = "O nome da igreja é obrigatório.")]
        [StringLength(200, ErrorMessage = "O campo Nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "O campo Cidade deve ter no máximo 100 caracteres.")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "Informe a UF com 2 letras.")]
        public string? Estado { get; set; }

        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail deve ter no máximo 100 caracteres.")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "O campo Telefone deve ter no máximo 20 caracteres.")]
        public string? Telefone { get; set; }
    }
}
