using KoinoniaHub.API.Dominio.Entidades;

namespace KoinoniaHub.API.Aplicacao.Regras
{
    // Corretiva G6 (RF9, RNF 9.4 proposta): pessoa inativa não recebe acesso nem função. A
    // inativação (RNF 9.3) encerra matrículas e atribuições e inativa a conta; enquanto o
    // cadastro estiver inativo, nada disso volta: criar ou reativar a conta, criar ou reativar
    // uma atribuição e matricular são recusados, e o caminho é reativar o cadastro em Pessoas.
    public static class PessoaInativa
    {
        public const string Mensagem = "Esta pessoa está inativa. Reative o cadastro em Pessoas antes.";

        public static bool Esta(Pessoa? pessoa) =>
            pessoa is not null && string.Equals(pessoa.Situacao, "Inativo", StringComparison.OrdinalIgnoreCase);

        public static void Recusar(Pessoa? pessoa)
        {
            if (Esta(pessoa))
                throw new InvalidOperationException(Mensagem);
        }
    }
}
