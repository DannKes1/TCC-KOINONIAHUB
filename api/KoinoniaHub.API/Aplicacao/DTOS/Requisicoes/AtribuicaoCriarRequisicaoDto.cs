using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Aplicacao.DTOs.Validacao;

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

        // RF21: Professor ou Auxiliar (lista fechada, Dominio/Regras/FuncoesAtribuicao).
        [Required]
        [StringLength(50)]
        [FuncaoAtribuicao]
        public string Funcao { get; set; } = "Professor";

        public DateTime? DataInicio { get; set; }

        public bool Ativo { get; set; } = true;
    }
}