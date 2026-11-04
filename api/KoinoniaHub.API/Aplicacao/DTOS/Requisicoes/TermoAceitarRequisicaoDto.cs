using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class TermoAceitarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Versão é obrigatório.")]
        [StringLength(20, ErrorMessage = "O campo Versão deve ter no máximo 20 caracteres.")]
        public string Versao { get; set; } = string.Empty;
    }
}
