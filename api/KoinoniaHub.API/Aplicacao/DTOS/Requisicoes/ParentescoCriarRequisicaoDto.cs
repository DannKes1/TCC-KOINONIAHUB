using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class ParentescoCriarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Parente é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ParenteId inválido.")]
        public int ParenteId { get; set; }

        [Required(ErrorMessage = "O campo Tipo de relacionamento é obrigatório.")]
        [StringLength(50, ErrorMessage = "O campo Tipo de relacionamento deve ter no máximo 50 caracteres.")]
        public string TipoRelacionamento { get; set; } = "Parente";
    }
}