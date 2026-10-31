using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Aplicacao.DTOs.Validacao;

namespace KoinoniaHub.API.Aplicacao.DTOs.Requisicoes
{
    public class AtribuicaoAtualizarRequisicaoDto
    {
        // RF21: Professor ou Auxiliar (lista fechada, Dominio/Regras/FuncoesAtribuicao).
        [Required]
        [StringLength(50)]
        [FuncaoAtribuicao]
        public string Funcao { get; set; } = "Professor";

        public bool Ativo { get; set; } = true;

        public DateTime? DataFim { get; set; }
    }
}