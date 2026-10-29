using System.ComponentModel.DataAnnotations;

namespace KoinoniaHub.API.Aplicacao.DTOS.Requisicoes
{
    // RF4 / RNF 4.3: mesmos limites e formato do cadastro de Pessoa (PessoaAtualizarRequisicaoDto),
    // para que entrada inválida volte como 400 por campo e não chegue ao banco.
    public class MeusDadosAtualizarRequisicaoDto
    {
        private string? _email;

        [StringLength(20)]
        public string? Celular { get; set; }

        // O e-mail é opcional: vazio ou só espaços vira nulo antes da validação, porque o
        // [EmailAddress] do .NET só aceita nulo ou texto com um único "@" fora das pontas
        // (a string vazia seria recusada). O front aplica a mesma regra.
        [EmailAddress]
        [StringLength(100)]
        public string? Email
        {
            get => _email;
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [StringLength(500)]
        public string? Endereco { get; set; }

        [StringLength(100)]
        public string? Bairro { get; set; }

        [StringLength(100)]
        public string? Cidade { get; set; }

        [StringLength(2)]
        public string? Estado { get; set; }

        [StringLength(10)]
        public string? CEP { get; set; }
    }
}
