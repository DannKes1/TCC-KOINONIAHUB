namespace KoinoniaHub.API.Aplicacao.Excecoes
{
    // Etapa 7.1: turma, matéria ou aula que não existe para a igreja do token. Lançada
    // pelos guardas de AutorizacaoEbdServico antes de qualquer verificação de perfil ou
    // atribuição e traduzida em 404 { mensagem } pelo RecursoNaoEncontradoFiltro, para
    // qualquer perfil. Deriva de Exception (não de InvalidOperationException) de
    // propósito: os catch dos controllers continuam devolvendo 400 só para regra de
    // negócio, e esta passa por eles até o filtro.
    public class RecursoNaoEncontradoException : Exception
    {
        public RecursoNaoEncontradoException(string mensagem) : base(mensagem)
        {
        }
    }
}
