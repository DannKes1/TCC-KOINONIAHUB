using System.ComponentModel.DataAnnotations;
using KoinoniaHub.API.Aplicacao.Regras;

namespace KoinoniaHub.API.Aplicacao.DTOS.Requisicoes
{
    // RF4 / RNF 4.3: mesmos limites e formato do cadastro de Pessoa (PessoaAtualizarRequisicaoDto),
    // para que entrada inválida volte como 400 por campo e não chegue ao banco.
    public class MeusDadosAtualizarRequisicaoDto
    {
        private string? _email;

        [StringLength(20, ErrorMessage = "O campo Celular deve ter no máximo 20 caracteres.")]
        public string? Celular { get; set; }

        // O e-mail é opcional: vazio ou só espaços vira nulo antes da validação, porque o
        // [EmailAddress] do .NET só aceita nulo ou texto com um único "@" fora das pontas
        // (a string vazia seria recusada). O front aplica a mesma regra.
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(100, ErrorMessage = "O campo E-mail deve ter no máximo 100 caracteres.")]
        public string? Email
        {
            get => _email;
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [StringLength(500, ErrorMessage = "O campo Endereço deve ter no máximo 500 caracteres.")]
        public string? Endereco { get; set; }

        [StringLength(100, ErrorMessage = "O campo Bairro deve ter no máximo 100 caracteres.")]
        public string? Bairro { get; set; }

        [StringLength(100, ErrorMessage = "O campo Cidade deve ter no máximo 100 caracteres.")]
        public string? Cidade { get; set; }

        [StringLength(2, ErrorMessage = "O campo UF deve ter no máximo 2 caracteres.")]
        public string? Estado { get; set; }

        [StringLength(10, ErrorMessage = "O campo CEP deve ter no máximo 10 caracteres.")]
        [RegularExpression(Cep.Padrao, ErrorMessage = Cep.Mensagem)]
        public string? CEP { get; set; }
    }
}
