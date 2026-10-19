using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KoinoniaHub.API.Aplicacao.Excecoes
{
    // Etapa 7.1 (decisão do autor): aula, matéria ou turma inexistente devolve 404
    // { mensagem } para qualquer perfil. Antes, os guardas lançavam
    // InvalidOperationException e o Professor recebia 400, enquanto a gestão (que
    // pula os guardas) recebia 404 do serviço. Registrado como filtro global em
    // Program.cs, como o ExigeAceiteTermoFiltro: um lugar só, sem catch por action.
    public sealed class RecursoNaoEncontradoFiltro : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not RecursoNaoEncontradoException excecao)
                return;

            context.Result = new NotFoundObjectResult(new { mensagem = excecao.Message });
            context.ExceptionHandled = true;
        }
    }
}
