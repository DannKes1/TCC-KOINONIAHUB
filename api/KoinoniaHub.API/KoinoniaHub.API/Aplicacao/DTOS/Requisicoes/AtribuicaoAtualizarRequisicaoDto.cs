using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class AtribuicaoAtualizarRequisicaoDto
    {
        [Required]
        [StringLength(50)]
        public string Funcao { get; set; } = "Professor";

        public bool Ativo { get; set; } = true;

        public DateTime? DataFim { get; set; }
    }
}