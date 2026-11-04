using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class DepartamentoCriarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O campo Nome deve ter no máximo 100 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "O campo Tipo deve ter no máximo 50 caracteres.")]
        public string Tipo { get; set; } = "EBD"; // EBD, Ministerio, Grupo

        [StringLength(500, ErrorMessage = "O campo Descrição deve ter no máximo 500 caracteres.")]
        public string? Descricao { get; set; }

        [StringLength(500, ErrorMessage = "O campo Imagem (URL) deve ter no máximo 500 caracteres.")]
        public string? ImagemUrl { get; set; }

        public bool Ativo { get; set; } = true;
    }
}