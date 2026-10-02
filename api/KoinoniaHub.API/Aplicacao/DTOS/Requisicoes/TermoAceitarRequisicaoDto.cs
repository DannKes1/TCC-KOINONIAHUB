using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class TermoAceitarRequisicaoDto
    {
        [Required]
        [StringLength(20)]
        public string Versao { get; set; } = string.Empty;
    }
}
