using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class ParentescoCriarRequisicaoDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ParenteId inválido.")]
        public int ParenteId { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoRelacionamento { get; set; } = "Parente";
    }
}