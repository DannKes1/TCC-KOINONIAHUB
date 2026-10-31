using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Dominio.Regras;

namespace KoinoniaHub.API.Aplicacao.DTOs.Validacao
{
    // RF21: função da atribuição em lista fechada. Valor fora de Professor/Auxiliar volta
    // como 400 ValidationProblemDetails em errors.Funcao, antes de chegar ao serviço.
    // Nulo é deixado para o [Required] do próprio DTO.
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class FuncaoAtribuicaoAttribute : ValidationAttribute
    {
        public FuncaoAtribuicaoAttribute() : base(FuncoesAtribuicao.MensagemInvalida)
        {
        }

        public override bool IsValid(object? value)
        {
            return value is null || (value is string texto && FuncoesAtribuicao.EhValida(texto));
        }
    }
}
