using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class MateriaCriarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        [StringLength(200, ErrorMessage = "O campo Nome deve ter no máximo 200 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "O campo Descrição deve ter no máximo 1000 caracteres.")]
        public string? Descricao { get; set; }

        [StringLength(500, ErrorMessage = "O campo Imagem (URL) deve ter no máximo 500 caracteres.")]
        public string? ImagemUrl { get; set; }

        public int OrdemExibicao { get; set; } = 0;

        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "O campo Turma é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "Informe a turma.")]
        public int DepartamentoId { get; set; }
    }
}