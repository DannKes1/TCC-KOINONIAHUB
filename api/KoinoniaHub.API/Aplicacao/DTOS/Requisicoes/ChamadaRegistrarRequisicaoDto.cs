using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class ChamadaRegistrarRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Itens da chamada é obrigatório.")]
        public List<ItemChamadaRequisicaoDto> Itens { get; set; } = new();

        // Visitantes avulsos do dia; null = não alterar o valor atual
        [Range(0, 999, ErrorMessage = "O campo Quantidade de visitantes deve estar entre 0 e 999.")]
        public int? QuantidadeVisitantes { get; set; }
    }

    public class ItemChamadaRequisicaoDto
    {
        [Required(ErrorMessage = "O campo Matrícula é obrigatório.")]
        public int AlunoDepartamentoId { get; set; } // matrícula

        // RF32 / RNF 32.6: presença ou ausência é registro explícito; anulável para o
        // [Required] valer — item sem o campo volta 400 em vez de virar ausência.
        [Required(ErrorMessage = "Informe se o aluno está presente.")]
        public bool? Presente { get; set; }

        [StringLength(500, ErrorMessage = "O campo Observação deve ter no máximo 500 caracteres.")]
        public string? Observacao { get; set; }
    }
}