using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    // RF44 — Editar Dados da Igreja: os cinco campos do requisito. Endereco, CEP, CNPJ e
    // LogoUrl existem na entidade, mas ficam fora por decisão do autor (motivação do RF é
    // o canal de contato exibido no Termo de Uso e Sigilo, RNF 42.7).
    public class IgrejaAtualizarRequisicaoDto
    {
        [Required(ErrorMessage = "O nome da igreja é obrigatório.")]
        [StringLength(200)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "Informe a UF com 2 letras.")]
        public string? Estado { get; set; }

        [EmailAddress(ErrorMessage = "E-mail em formato inválido.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Telefone { get; set; }
    }
}
