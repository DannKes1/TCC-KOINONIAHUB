using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class ChamadaRegistrarRequisicaoDto
    {
        [Required]
        public List<ItemChamadaRequisicaoDto> Itens { get; set; } = new();

        // Visitantes avulsos do dia; null = não alterar o valor atual
        [Range(0, 999)]
        public int? QuantidadeVisitantes { get; set; }
    }

    public class ItemChamadaRequisicaoDto
    {
        [Required]
        public int AlunoDepartamentoId { get; set; } // matrícula

        public bool Presente { get; set; }

        [StringLength(500)]
        public string? Observacao { get; set; }
    }
}