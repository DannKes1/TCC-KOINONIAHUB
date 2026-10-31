namespace KoinoniaHub.API.Dominio.Regras
{
    // RF21: a atribuição vincula uma pessoa a uma função da turma, em lista fechada
    // (Professor ou Auxiliar). É a mesma lista que a autorização por turma (RNF 21.4) e
    // Minhas Turmas usam para reconhecer quem opera a turma; valor fora dela não é gravado.
    public static class FuncoesAtribuicao
    {
        public const string Professor = "Professor";
        public const string Auxiliar = "Auxiliar";

        public static readonly string[] Todas = { Professor, Auxiliar };

        public const string MensagemInvalida = "Função inválida: use Professor ou Auxiliar.";

        // Comparação exata: a lista é fechada e a tela só oferece os dois valores.
        public static bool EhValida(string? funcao)
        {
            return funcao is not null && Array.IndexOf(Todas, funcao) >= 0;
        }
    }
}
