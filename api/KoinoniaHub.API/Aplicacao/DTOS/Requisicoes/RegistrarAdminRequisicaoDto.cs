using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class RegistrarAdminRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Igreja é obrigatório.")]
        public IgrejaCriarRequisicaoDto Igreja { get; set; } = new();

        [Required(ErrorMessage = "O campo E-mail do administrador é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail do administrador deve ter no máximo 100 caracteres.")]
        public string EmailAdmin { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Senha do administrador é obrigatório.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "O campo Senha do administrador deve ter entre 6 e 100 caracteres.")]
        public string SenhaAdmin { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo Nome do administrador é obrigatório.")]
        [StringLength(200, ErrorMessage = "O campo Nome do administrador deve ter no máximo 200 caracteres.")]
        public string NomeAdmin { get; set; } = string.Empty;

        // RNF 1.5 / 42.1: versão do Termo de Uso e Sigilo aceita no cadastro inicial.
        // Sem a versão vigente, a API recusa com 400 e nada é criado.
        [StringLength(20, ErrorMessage = "O campo Versão do termo deve ter no máximo 20 caracteres.")]
        public string? AceiteTermoVersao { get; set; }
    }
}
