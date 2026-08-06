using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class AtribuicaoCriarRequisicaoDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "PessoaId inválido.")]
        public int PessoaId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "DepartamentoId inválido.")]
        public int DepartamentoId { get; set; }

        [Required]
        [StringLength(50)]
        public string Funcao { get; set; } = "Professor";

        public DateTime? DataInicio { get; set; }

        public bool Ativo { get; set; } = true;
    }
}